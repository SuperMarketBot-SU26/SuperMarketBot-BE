using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Staff;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>Flow 4 — Out-of-Stock Handler.</summary>
public sealed class StaffService(AppDbContext db, ILocalizationService localizer) : IStaffService
{
    public async Task<ReportOosResponseDto> ReportOutOfStockAsync(
        ReportOosRequestDto request, CancellationToken ct = default)
    {
        // Tìm Slot và Aisle liên kết
        var slot = await db.Slots
            .AsNoTracking()
            .Include(s => s.Shelf)
                .ThenInclude(sl => sl.Aisle)
            .FirstOrDefaultAsync(s => s.SlotId == request.SlotId, ct)
            ?? throw new KeyNotFoundException(localizer.Get("SlotNotFound", request.SlotId));

        int aisleId = slot.Shelf.Aisle.AisleId;

        // Tính tổng tồn kho còn lại của sản phẩm này trên toàn bộ kệ
        // Lấy ProductId qua ProductSlot (N-N)
        var productId = await db.ProductSlots
            .AsNoTracking()
            .Where(ps => ps.SlotId == request.SlotId)
            .Select(ps => (int?)ps.ProductId)
            .FirstOrDefaultAsync(ct);

        int totalWarehouseStock = 0;
        if (productId.HasValue)
        {
            totalWarehouseStock = await db.ProductSlots
                .AsNoTracking()
                .Where(ps => ps.ProductId == productId.Value && ps.SlotId != request.SlotId)
                .Join(db.Slots, ps => ps.SlotId, s => s.SlotId, (ps, s) => s)
                .SumAsync(s => s.Quantity, ct);
        }

        bool hasWarehouseStock = totalWarehouseStock > 0;

        // Ghi nhận bản quét kệ
        var scan = new Domain.Entities.AisleScan
        {
            AisleId = aisleId,
            RobotId = request.RobotId,
            ImageUrl = request.ImageUrl,
            EmptyPercentage = request.EmptyPercentage,
            ScannedAt = VnDateTime.Now
        };
        db.AisleScans.Add(scan);
        await db.SaveChangesAsync(ct);

        string message = hasWarehouseStock
            ? localizer.Get("OosEventWithStock", scan.ScanId)
            : localizer.Get("OosEventNoStock", scan.ScanId);

        return new ReportOosResponseDto(scan.ScanId, scan.EmptyPercentage > 30, hasWarehouseStock, message);
    }

    public async Task<RestockTaskListResponseDto> GetRestockTasksAsync(CancellationToken ct = default)
    {
        var tasks = await db.AisleScans
            .AsNoTracking()
            .Where(ss => ss.EmptyPercentage > 30)
            .OrderByDescending(ss => ss.EmptyPercentage)
            .Take(30)
            .Join(db.Aisles, ss => ss.AisleId, a => a.AisleId, (ss, a) => new { ss, a })
            .Join(db.Zones, x => x.a.ZoneId, z => z.ZoneId, (x, z) => new { x.ss, x.a, z })
            .Select(x => new RestockTaskDto(
                x.ss.ScanId,
                0,  // SlotId — enriched lazily if needed
                "-",
                $"Khu {x.z.ZoneCode} - Dãy {x.a.AisleCode}",
                0,
                "Unknown",
                x.ss.ImageUrl,
                0,
                x.ss.EmptyPercentage,
                x.ss.ScannedAt,
                x.ss.EmptyPercentage >= 80 ? "High" : x.ss.EmptyPercentage >= 50 ? "Medium" : "Low",
                true))
            .ToListAsync(ct);

        return new RestockTaskListResponseDto(tasks.Count, tasks);
    }

    public async Task CompleteRestockAsync(CompleteRestockRequestDto request, CancellationToken ct = default)
    {
        var slot = await db.Slots.FindAsync([request.SlotId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("SlotNotFound", request.SlotId));

        if (request.QuantityAdded <= 0)
            throw new ArgumentException("QuantityAdded must be >= 1.", nameof(request));

        slot.Quantity += request.QuantityAdded;
        slot.LastScannedAt = VnDateTime.Now;
        await db.SaveChangesAsync(ct);
    }
}
