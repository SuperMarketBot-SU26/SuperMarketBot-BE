using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class ZoneAisleService(AppDbContext db) : IZoneAisleService
{
    public async Task<IReadOnlyList<ZoneDto>> GetZonesAsync(int? floorId = null, CancellationToken ct = default)
    {
        var query = db.Zones.AsNoTracking();

        if (floorId.HasValue)
            query = query.Where(z => z.FloorId == floorId.Value);

        return await query
            .OrderBy(z => z.ZoneName)
            .Select(z => new ZoneDto(z.ZoneId, z.FloorId, z.ZoneName, z.Description))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AisleDto>> GetAislesAsync(int? zoneId = null, CancellationToken ct = default)
    {
        var query = db.Aisles.AsNoTracking();

        if (zoneId.HasValue)
            query = query.Where(a => a.ZoneId == zoneId.Value);

        return await query
            .OrderBy(a => a.AisleCode)
            .Select(a => new AisleDto(a.AisleId, a.ZoneId, a.AisleCode, a.AisleName))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AisleDensityDto>> GetAisleDensitiesAsync(
        int? zoneId = null, CancellationToken ct = default)
    {
        // Lấy tất cả Aisle (có lọc zone nếu cần)
        var aisleQuery = db.Aisles.AsNoTracking();
        if (zoneId.HasValue)
            aisleQuery = aisleQuery.Where(a => a.ZoneId == zoneId.Value);

        var aisles = await aisleQuery
            .OrderBy(a => a.AisleCode)
            .ToListAsync(ct);

        if (aisles.Count == 0)
            return [];

        var aisleIds = aisles.Select(a => a.AisleId).ToList();

        // Lấy AisleScan gần nhất của mỗi Aisle
        var latestScans = await db.AisleScans
            .AsNoTracking()
            .Where(s => aisleIds.Contains(s.AisleId))
            .GroupBy(s => s.AisleId)
            .Select(g => g.OrderByDescending(s => s.ScannedAt).First())
            .ToListAsync(ct);

        var scanByAisle = latestScans.ToDictionary(s => s.AisleId);

        return aisles.Select(a =>
        {
            scanByAisle.TryGetValue(a.AisleId, out var scan);

            var density = scan?.DensityPercentage ?? 100m;
            var empty   = scan?.EmptyPercentage   ?? 0m;

            // Màu hiển thị theo mật độ
            var color = density >= 70m ? "green"
                      : density >= 40m ? "yellow"
                      : "red";

            return new AisleDensityDto(
                AisleId:           a.AisleId,
                AisleCode:         a.AisleCode,
                AisleName:         a.AisleName,
                LatestScanId:      scan?.ScanId,
                ScannedAt:         scan?.ScannedAt,
                DensityPercentage: density,
                EmptyPercentage:   empty,
                NeedsRestock:      scan?.NeedsRestock ?? false,
                ImageUrl:          scan?.ImageUrl,
                DensityColor:      color);
        }).ToList();
    }
}
