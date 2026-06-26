using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.AisleScans;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Options;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AisleScanService(
    AppDbContext dbContext,
    ICloudStorageService cloudStorage,
    IOptions<CloudinaryOptions> cloudinaryOptions,
    IAiVisionProxy aiVisionProxy,
    ILogger<AisleScanService> logger) : IAisleScanService
{
    private readonly CloudinaryOptions _cloudOpts = cloudinaryOptions.Value;

    public async Task<IReadOnlyList<ShelfScanDto>> GetRecentScansAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 20;
        }

        return await dbContext.AisleScans
            .AsNoTracking()
            .OrderByDescending(x => x.ScannedAt)
            .Take(take)
            .Select(x => new ShelfScanDto(
                x.ScanId,
                x.AisleId,
                null,
                x.RobotId,
                x.ScannedAt,
                x.EmptyPercentage,
                x.DensityPercentage,
                x.NeedsRestock,
                x.ImageUrl,
                x.AisleNodeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShelfScanDto> CreateScanAsync(CreateAisleScanRequestDto request, CancellationToken cancellationToken = default)
    {
        string? imageUrl = request.ImageUrl;
        decimal emptyPct = request.EmptyPercentage ?? await ComputeEmptyPercentageFromSlotsAsync(request.AisleId, cancellationToken);
        decimal densityPct = Math.Clamp(100m - emptyPct, 0m, 100m);

        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            var fileName = $"aisle-{request.AisleId}-robot-{request.RobotId}-{Guid.NewGuid():N}";
            imageUrl = await cloudStorage.UploadBase64Async(
                request.ImageBase64,
                _cloudOpts.AisleScansFolder,
                fileName,
                cancellationToken);

            var aiDensity = await AnalyzeImageForDensityAsync(request.ImageBase64, fileName, cancellationToken);
            if (aiDensity.HasValue)
            {
                densityPct = Math.Clamp(aiDensity.Value, 0m, 100m);
                emptyPct = Math.Clamp(100m - densityPct, 0m, 100m);
            }
            else if (request.EmptyPercentage is not null)
            {
                emptyPct = request.EmptyPercentage.Value;
                densityPct = Math.Clamp(100m - emptyPct, 0m, 100m);
            }
        }
        else if (request.EmptyPercentage is not null)
        {
            emptyPct = request.EmptyPercentage.Value;
            densityPct = Math.Clamp(100m - emptyPct, 0m, 100m);
        }

        var entity = new AisleScan
        {
            AisleId = request.AisleId,
            AisleNodeId = request.AisleNodeId,
            RobotId = request.RobotId,
            EmptyPercentage = Math.Round(emptyPct, 2),
            DensityPercentage = Math.Round(densityPct, 2),
            ImageUrl = imageUrl,
            ScannedAt = VnDateTime.Now,
            NeedsRestock = densityPct < 70m || emptyPct > 30m
        };

        dbContext.AisleScans.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[AisleScanService] Persisted scan: scanId={ScanId}, aisleId={AisleId}, aisleNodeId={AisleNodeId}, densityPct={DensityPct}%, imageUrl={ImageUrl}",
            entity.ScanId,
            entity.AisleId,
            entity.AisleNodeId,
            entity.DensityPercentage,
            entity.ImageUrl);

        return new ShelfScanDto(
            entity.ScanId,
            entity.AisleId,
            null,
            entity.RobotId,
            entity.ScannedAt,
            entity.EmptyPercentage,
            entity.DensityPercentage,
            entity.NeedsRestock,
            entity.ImageUrl,
            entity.AisleNodeId);
    }

    private async Task<decimal?> AnalyzeImageForDensityAsync(string imageBase64, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var imageBytes = Convert.FromBase64String(imageBase64);
            var analysisJson = await aiVisionProxy.AnalyzeImageAsync(imageBytes, fileName, cancellationToken);
            if (string.IsNullOrWhiteSpace(analysisJson))
            {
                return null;
            }

            using var document = JsonDocument.Parse(analysisJson);
            var root = document.RootElement;
            return FindDensityPercentage(root);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[AisleScanService] AI density analysis failed, using fallback calculation");
            return null;
        }
    }

    private static decimal? FindDensityPercentage(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in new[] { "densityPercentage", "remainingDensityPercentage", "stockPercentage", "availableDensityPercentage", "density", "stockDensity" })
            {
                if (element.TryGetProperty(candidate, out var property) && TryReadDecimal(property, out var value))
                {
                    return value;
                }
            }

            foreach (var child in element.EnumerateObject())
            {
                var nested = FindDensityPercentage(child.Value);
                if (nested.HasValue)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindDensityPercentage(item);
                if (nested.HasValue)
                {
                    return nested;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
        {
            return number;
        }

        return null;
    }

    private static bool TryReadDecimal(JsonElement element, out decimal value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), out value))
        {
            return true;
        }

        value = 0m;
        return false;
    }

    /// <summary>
    /// Tính % trống trung bình của tất cả Slot thuộc các Shelf của một Aisle.
    /// Công thức: (số slot Quantity = 0) / (tổng slot) × 100.
    /// </summary>
    private async Task<decimal> ComputeEmptyPercentageFromSlotsAsync(int aisleId, CancellationToken ct)
    {
        var slotStats = await dbContext.Shelves
            .AsNoTracking()
            .Where(sh => sh.AisleId == aisleId)
            .SelectMany(sh => sh.Slots)
            .GroupBy(s => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Empty = g.Count(s => s.Quantity <= 0)
            })
            .FirstOrDefaultAsync(ct);

        if (slotStats == null || slotStats.Total == 0)
            return 0m;

        return Math.Round((decimal)slotStats.Empty * 100m / slotStats.Total, 2);
    }
}
