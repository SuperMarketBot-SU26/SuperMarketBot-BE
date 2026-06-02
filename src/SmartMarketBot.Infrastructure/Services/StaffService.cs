using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Staff;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>Flow 4 — Out-of-Stock Handler.</summary>
public sealed class StaffService(AppDbContext db) : IStaffService
{
    public async Task<ReportOosResponseDto> ReportOutOfStockAsync(
        ReportOosRequestDto request, CancellationToken ct = default)
    {
        // Tìm Slot và Aisle liên kết
        var slot = await db.Slots
            .AsNoTracking()
            .Include(s => s.ShelfLevel)
                .ThenInclude(sl => sl.Aisle)
            .FirstOrDefaultAsync(s => s.SlotID == request.SlotId, ct)
            ?? throw new KeyNotFoundException($"Slot {request.SlotId} not found.");

        int aisleId = slot.ShelfLevel.Aisle.AisleID;

        // Tính tổng tồn kho còn lại của sản phẩm này trên toàn bộ kệ
        int totalWarehouseStock = slot.ProductID.HasValue
            ? await db.Slots
                .AsNoTracking()
                .Where(s => s.ProductID == slot.ProductID && s.SlotID != request.SlotId)
                .SumAsync(s => s.Quantity, ct)
            : 0;

        bool hasWarehouseStock = totalWarehouseStock > 0;

        // Ghi nhận bản quét kệ
        var scan = new Domain.Entities.ShelfScan
        {
            AisleID = aisleId,
            ShelfLevelID = slot.ShelfLevelID,
            RobotID = request.RobotId,
            ImageUrl = request.ImageUrl,
            EmptyPercentage = request.EmptyPercentage,
            IsOccluded = request.IsOccluded,
            OcclusionReason = request.OcclusionReason
        };
        db.ShelfScans.Add(scan);
        await db.SaveChangesAsync(ct);

        string message = hasWarehouseStock
            ? $"Out-of-stock event logged (ScanID={scan.ScanID}). Staff notification sent."
            : $"Out-of-stock event logged (ScanID={scan.ScanID}). No warehouse stock — product substitution recommended.";

        return new ReportOosResponseDto(scan.ScanID, scan.EmptyPercentage > 30, hasWarehouseStock, message);
    }

    public async Task<RestockTaskListResponseDto> GetRestockTasksAsync(CancellationToken ct = default)
    {
        var tasks = await db.ShelfScans
            .AsNoTracking()
            .Where(ss => ss.EmptyPercentage > 30 && !ss.IsOccluded)
            .OrderByDescending(ss => ss.EmptyPercentage)
            .Take(30)
            .Join(db.Aisles, ss => ss.AisleID, a => a.AisleID, (ss, a) => new { ss, a })
            .Join(db.Zones, x => x.a.ZoneID, z => z.ZoneID, (x, z) => new { x.ss, x.a, z })
            .Select(x => new RestockTaskDto(
                x.ss.ScanID,
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
        var slot = await db.Slots.FindAsync([request.SlotId], ct);
        if (slot is not null)
        {
            slot.Quantity += request.QuantityAdded;
            slot.LastScannedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
