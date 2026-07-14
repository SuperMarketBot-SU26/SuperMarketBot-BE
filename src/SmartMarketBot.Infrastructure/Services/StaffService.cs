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
                .ThenInclude(sl => sl!.Aisle)
            .FirstOrDefaultAsync(s => s.SlotId == request.SlotId, ct)
            ?? throw new KeyNotFoundException(localizer.Get("SlotNotFound", request.SlotId));

        var shelf = slot.Shelf
            ?? throw new InvalidOperationException(localizer.Get("SlotHasNoShelf", slot.SlotId));
        var aisle = shelf.Aisle
            ?? throw new InvalidOperationException(localizer.Get("ShelfHasNoAisle", shelf.ShelfId));
        int aisleId = aisle.AisleId;

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
            .Where(ss => ss.NeedsRestock)
            .OrderByDescending(ss => ss.EmptyPercentage)
            .Take(30)
            .Join(db.Aisles, ss => ss.AisleId, a => a.AisleId, (ss, a) => new { ss, a })
            .Join(db.Zones, x => x.a.ZoneId, z => z.ZoneId, (x, z) => new { x.ss, x.a, z })
            .Select(x => new RestockTaskDto(
                x.ss.ScanId,
                0,  // SlotId — enriched lazily if needed
                "-",
                $"Khu {x.z.ZoneName ?? x.z.ZoneId.ToString()} - Dãy {x.a.AisleCode}",
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

    public async Task<int> CompleteRestockAsync(CompleteRestockRequestDto request, CancellationToken ct = default)
    {
        // Tìm toàn bộ các bản quét chưa hoàn tất (NeedsRestock = true) tại vị trí lối đi và node được truyền lên
        var query = db.AisleScans.Where(ss => ss.AisleId == request.AisleId && ss.NeedsRestock);

        if (request.AisleNodeId.HasValue)
        {
            query = query.Where(ss => ss.AisleNodeId == request.AisleNodeId.Value);
        }
        else
        {
            query = query.Where(ss => ss.AisleNodeId == null);
        }

        var activeScans = await query.ToListAsync(ct);

        // Đóng toàn bộ các nhiệm vụ quét kệ tại vị trí này
        foreach (var scan in activeScans)
        {
            scan.NeedsRestock = false;
        }

        // Nếu client truyền thêm thông tin SlotId và QuantityAdded thì cập nhật số lượng tồn kho của Slot
        if (request.SlotId.HasValue && request.SlotId.Value > 0 && request.QuantityAdded.HasValue && request.QuantityAdded.Value > 0)
        {
            var slot = await db.Slots.FindAsync([request.SlotId.Value], ct);
            if (slot != null)
            {
                slot.Quantity += request.QuantityAdded.Value;
                slot.LastScannedAt = VnDateTime.Now;
            }
        }

        await db.SaveChangesAsync(ct);
        return activeScans.Count;
    }
}
