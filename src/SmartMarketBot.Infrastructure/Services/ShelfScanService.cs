using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.AisleScans;
using SmartMarketBot.Application.Models.Realtime;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Options;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AisleScanService(
    AppDbContext dbContext,
    ICloudStorageService cloudStorage,
    IOptions<CloudinaryOptions> cloudinaryOptions,
    IStaffRealtimeNotifier staffNotifier,
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
                x.NeedsRestock,
                x.ImageUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShelfScanDto> CreateScanAsync(CreateAisleScanRequestDto request, CancellationToken cancellationToken = default)
    {
        // Upload ảnh Base64 lên Cloudinary nếu có
        string? imageUrl = request.ImageUrl;
        if (!string.IsNullOrEmpty(request.ImageBase64))
        {
            var fileName = $"aisle-{request.AisleId}-robot-{request.RobotId}-{Guid.NewGuid():N}";
            imageUrl = await cloudStorage.UploadBase64Async(
                request.ImageBase64,
                _cloudOpts.AisleScansFolder,
                fileName,
                cancellationToken);
        }

        // Nếu client không cung cấp EmptyPercentage → tự tính từ trung bình Slot.Quantity của toàn bộ Shelf thuộc Aisle
        decimal emptyPct = request.EmptyPercentage ?? await ComputeEmptyPercentageFromSlotsAsync(request.AisleId, cancellationToken);

        var entity = new AisleScan
        {
            AisleId = request.AisleId,
            RobotId = request.RobotId,
            EmptyPercentage = emptyPct,
            ImageUrl = imageUrl,
            ScannedAt = VnDateTime.Now,
            NeedsRestock = emptyPct > 30m
        };

        dbContext.AisleScans.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Broadcast tới Staff Hub nếu vượt ngưỡng cần bổ sung hàng
        if (entity.NeedsRestock)
        {
            var aisle = await dbContext.Aisles
                .AsNoTracking()
                .Include(a => a.Zone)
                .FirstOrDefaultAsync(a => a.AisleId == entity.AisleId, cancellationToken);

            var location = aisle is null
                ? $"Aisle #{entity.AisleId}"
                : $"Khu {aisle.Zone?.ZoneName ?? aisle.ZoneId.ToString()} - Dãy {aisle.AisleCode}";

            var severity = entity.EmptyPercentage >= 80 ? "critical"
                          : entity.EmptyPercentage >= 50 ? "warning" : "info";

            var alert = new StaffRealtimeAlertDto(
                AlertId: entity.ScanId,
                AlertType: "RestockTask",
                Severity: severity,
                Title: $"Kệ trống {entity.EmptyPercentage:F1}% - cần bổ sung hàng",
                Message: $"{location}. Mật độ trống: {entity.EmptyPercentage:F1}%.",
                ZoneId: aisle?.ZoneId,
                AisleId: entity.AisleId,
                SlotId: null,
                RobotId: entity.RobotId,
                MemberId: null,
                Timestamp: DateTime.UtcNow);

            try
            {
                await staffNotifier.BroadcastAlertAsync(alert, cancellationToken);
                logger.LogInformation(
                    "[AisleScanService] Broadcasted staff alert: scanId={ScanId} pct={Pct}",
                    entity.ScanId, entity.EmptyPercentage);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[AisleScanService] Failed to broadcast staff alert");
            }
        }

        return new ShelfScanDto(
            entity.ScanId,
            entity.AisleId,
            null,
            entity.RobotId,
            entity.ScannedAt,
            entity.EmptyPercentage,
            entity.NeedsRestock,
            entity.ImageUrl);
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
