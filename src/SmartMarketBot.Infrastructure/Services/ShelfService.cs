using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class ShelfService(AppDbContext dbContext) : IShelfService
{
    // ─── Shelf ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ShelfSummaryDto>> GetShelvesAsync(int? aisleId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Shelves
            .AsNoTracking()
            .Include(s => s.Aisle)
                .ThenInclude(a => a!.Zone)
            .AsQueryable();

        if (aisleId.HasValue)
            query = query.Where(s => s.AisleId == aisleId.Value);

        var shelves = await query
            .OrderBy(s => s.Aisle!.AisleCode)
            .ThenBy(s => s.LevelNumber)
            .ToListAsync(cancellationToken);

        return shelves.Select(s => new ShelfSummaryDto(
            s.ShelfId,
            s.AisleId,
            s.Aisle!.AisleCode,
            s.Aisle.AisleName,
            s.LevelNumber))
            .ToList();
    }

    public async Task<ShelfDto?> GetShelfByIdAsync(int shelfId, CancellationToken cancellationToken = default)
    {
        var shelf = await dbContext.Shelves
            .AsNoTracking()
            .Include(s => s.Aisle)
                .ThenInclude(a => a!.Zone)
            .Include(s => s.Slots)
                .ThenInclude(sl => sl.ProductSlots)
                    .ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(s => s.ShelfId == shelfId, cancellationToken);

        if (shelf is null) return null;

        return ToDto(shelf);
    }

    public async Task<ShelfDto> CreateShelfAsync(CreateShelfRequestDto request, CancellationToken cancellationToken = default)
    {
        var aisleExists = await dbContext.Aisles
            .AsNoTracking()
            .AnyAsync(a => a.AisleId == request.AisleId, cancellationToken);

        if (!aisleExists)
            throw new KeyNotFoundException($"Aisle '{request.AisleId}' not found.");

        var shelf = new Shelf
        {
            AisleId = request.AisleId,
            LevelNumber = request.LevelNumber
        };

        var slotCount = request.SlotCount > 0 ? request.SlotCount : 1;
        for (int i = 1; i <= slotCount; i++)
        {
            shelf.Slots.Add(new Slot
            {
                SlotCode = string.IsNullOrWhiteSpace(request.ShelfLabel)
                    ? $"S{i}"
                    : $"{request.ShelfLabel}-{i}",
                Quantity = 0
            });
        }

        dbContext.Shelves.Add(shelf);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload với navigation properties
        var created = await dbContext.Shelves
            .Include(s => s.Aisle)
                .ThenInclude(a => a!.Zone)
            .Include(s => s.Slots)
                .ThenInclude(sl => sl.ProductSlots)
                    .ThenInclude(ps => ps.Product)
            .FirstAsync(s => s.ShelfId == shelf.ShelfId, cancellationToken);

        return ToDto(created);
    }

    public async Task<ShelfDto?> UpdateShelfAsync(int shelfId, UpdateShelfRequestDto request, CancellationToken cancellationToken = default)
    {
        var shelf = await dbContext.Shelves
            .Include(s => s.Aisle)
                .ThenInclude(a => a!.Zone)
            .Include(s => s.Slots)
                .ThenInclude(sl => sl.ProductSlots)
                    .ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(s => s.ShelfId == shelfId, cancellationToken);

        if (shelf is null) return null;

        if (request.LevelNumber.HasValue)
            shelf.LevelNumber = request.LevelNumber.Value;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(shelf);
    }

    public async Task<bool> DeleteShelfAsync(int shelfId, CancellationToken cancellationToken = default)
    {
        var shelf = await dbContext.Shelves.FindAsync(new object[] { shelfId }, cancellationToken);
        if (shelf is null) return false;

        dbContext.Shelves.Remove(shelf);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ─── Slot ────────────────────────────────────────────────────────────────

    public async Task<SlotDto?> GetSlotByIdAsync(int slotId, CancellationToken cancellationToken = default)
    {
        var slot = await dbContext.Slots
            .AsNoTracking()
            .Include(sl => sl.ProductSlots)
                .ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(sl => sl.SlotId == slotId, cancellationToken);

        if (slot is null) return null;
        return ToSlotDto(slot);
    }

    public async Task<SlotDto> CreateSlotAsync(CreateSlotRequestDto request, CancellationToken cancellationToken = default)
    {
        var shelfExists = await dbContext.Shelves
            .AsNoTracking()
            .AnyAsync(s => s.ShelfId == request.ShelfId, cancellationToken);

        if (!shelfExists)
            throw new KeyNotFoundException($"Shelf '{request.ShelfId}' not found.");

        var slot = new Slot
        {
            ShelfId = request.ShelfId,
            SlotCode = request.SlotCode,
            Quantity = 0
        };

        dbContext.Slots.Add(slot);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await dbContext.Slots
            .Include(sl => sl.ProductSlots)
                .ThenInclude(ps => ps.Product)
            .FirstAsync(sl => sl.SlotId == slot.SlotId, cancellationToken);

        return ToSlotDto(created);
    }

    public async Task<SlotDto?> UpdateSlotAsync(int slotId, UpdateSlotRequestDto request, CancellationToken cancellationToken = default)
    {
        var slot = await dbContext.Slots
            .Include(sl => sl.ProductSlots)
                .ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(sl => sl.SlotId == slotId, cancellationToken);

        if (slot is null) return null;

        if (!string.IsNullOrWhiteSpace(request.SlotCode))
            slot.SlotCode = request.SlotCode;

        if (request.Quantity.HasValue)
            slot.Quantity = request.Quantity.Value;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSlotDto(slot);
    }

    public async Task<bool> DeleteSlotAsync(int slotId, CancellationToken cancellationToken = default)
    {
        var slot = await dbContext.Slots.FindAsync(new object[] { slotId }, cancellationToken);
        if (slot is null) return false;

        dbContext.Slots.Remove(slot);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<SlotDto?> SetSlotQuantityAsync(SetSlotQuantityRequestDto request, CancellationToken cancellationToken = default)
    {
        var productSlot = await dbContext.ProductSlots
            .Include(ps => ps.Slot)
                .ThenInclude(sl => sl!.ProductSlots)
                    .ThenInclude(ps2 => ps2.Product)
            .Include(ps => ps.Product)
            .FirstOrDefaultAsync(ps => ps.SlotId == request.SlotId && ps.ProductId == request.ProductId, cancellationToken);

        if (productSlot is null)
        {
            // Nếu chưa có, tạo mới
            var slotExists = await dbContext.Slots.AnyAsync(s => s.SlotId == request.SlotId, cancellationToken);
            var productExists = await dbContext.Products.AnyAsync(p => p.ProductId == request.ProductId, cancellationToken);

            if (!slotExists) throw new KeyNotFoundException($"Slot '{request.SlotId}' not found.");
            if (!productExists) throw new KeyNotFoundException($"Product '{request.ProductId}' not found.");

            productSlot = new ProductSlot
            {
                SlotId = request.SlotId,
                ProductId = request.ProductId
            };
            dbContext.ProductSlots.Add(productSlot);
        }

        // Cập nhật tổng quantity
        var slot = await dbContext.Slots.FindAsync(new object[] { request.SlotId }, cancellationToken);
        if (slot is null) return null;

        // Tính lại tổng quantity = sum các ProductSlot
        var totalQuantity = await dbContext.ProductSlots
            .Where(ps => ps.SlotId == request.SlotId)
            .SumAsync(ps => ps.ProductId == request.ProductId ? request.Quantity : 0, cancellationToken);

        slot.Quantity = request.Quantity;
        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload
        var updated = await dbContext.Slots
            .Include(sl => sl.ProductSlots)
                .ThenInclude(ps => ps.Product)
            .FirstAsync(sl => sl.SlotId == request.SlotId, cancellationToken);

        return ToSlotDto(updated);
    }

    // ─── ProductSlot ─────────────────────────────────────────────────────────

    public async Task<SlotDto?> AssignProductToSlotAsync(AssignProductToSlotRequestDto request, CancellationToken cancellationToken = default)
    {
        var slotExists = await dbContext.Slots.AnyAsync(s => s.SlotId == request.SlotId, cancellationToken);
        if (!slotExists)
            throw new KeyNotFoundException($"Slot '{request.SlotId}' not found.");

        var productExists = await dbContext.Products.AnyAsync(p => p.ProductId == request.ProductId, cancellationToken);
        if (!productExists)
            throw new KeyNotFoundException($"Product '{request.ProductId}' not found.");

        var existing = await dbContext.ProductSlots
            .FirstOrDefaultAsync(ps => ps.SlotId == request.SlotId && ps.ProductId == request.ProductId, cancellationToken);

        if (existing is not null)
        {
            // Đã tồn tại, cập nhật quantity
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var productSlot = new ProductSlot
            {
                SlotId = request.SlotId,
                ProductId = request.ProductId
            };
            dbContext.ProductSlots.Add(productSlot);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Cập nhật tổng quantity của Slot
        var slot = await dbContext.Slots.FindAsync(new object[] { request.SlotId }, cancellationToken);
        if (slot is not null)
        {
            slot.Quantity = request.Quantity;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Reload
        var updated = await dbContext.Slots
            .Include(sl => sl.ProductSlots)
                .ThenInclude(ps => ps.Product)
            .FirstAsync(sl => sl.SlotId == request.SlotId, cancellationToken);

        return ToSlotDto(updated);
    }

    public async Task<SlotDto?> RemoveProductFromSlotAsync(RemoveProductFromSlotRequestDto request, CancellationToken cancellationToken = default)
    {
        var productSlot = await dbContext.ProductSlots
            .FirstOrDefaultAsync(ps => ps.SlotId == request.SlotId && ps.ProductId == request.ProductId, cancellationToken);

        if (productSlot is null)
            throw new KeyNotFoundException($"ProductSlot not found for Slot '{request.SlotId}' and Product '{request.ProductId}'.");

        dbContext.ProductSlots.Remove(productSlot);

        // Cập nhật tổng quantity của Slot
        var remainingCount = await dbContext.ProductSlots.CountAsync(ps => ps.SlotId == request.SlotId, cancellationToken);
        var slot = await dbContext.Slots.FindAsync(new object[] { request.SlotId }, cancellationToken);
        if (slot is not null)
        {
            slot.Quantity = remainingCount > 0 ? slot.Quantity - 1 : 0;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload
        var updated = await dbContext.Slots
            .Include(sl => sl.ProductSlots)
                .ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(sl => sl.SlotId == request.SlotId, cancellationToken);

        return updated is null ? null : ToSlotDto(updated);
    }

    public async Task<IReadOnlyList<SlotSummaryDto>> GetSlotsByShelfAsync(int shelfId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Slots
            .AsNoTracking()
            .Where(sl => sl.ShelfId == shelfId)
            .OrderBy(sl => sl.SlotCode)
            .Select(sl => new SlotSummaryDto(
                sl.SlotId,
                sl.ShelfId,
                sl.SlotCode,
                sl.Quantity,
                sl.LastScannedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SlotDto>> FindSlotsByProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var slots = await dbContext.Slots
            .AsNoTracking()
            .Include(sl => sl.ProductSlots)
                .ThenInclude(ps => ps.Product)
            .Include(sl => sl.Shelf)
                .ThenInclude(s => s!.Aisle)
            .Where(sl => sl.ProductSlots.Any(ps => ps.ProductId == productId))
            .ToListAsync(cancellationToken);

        return slots.Select(ToSlotDto).ToList();
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private static ShelfDto ToDto(Shelf shelf)
    {
        return new ShelfDto(
            shelf.ShelfId,
            shelf.AisleId,
            shelf.LevelNumber,
            shelf.Aisle?.AisleCode,
            shelf.Aisle?.AisleName,
            shelf.Aisle?.Zone?.ZoneName,
            shelf.Slots.Select(ToSlotDto).ToList());
    }

    private static SlotDto ToSlotDto(Slot slot)
    {
        return new SlotDto(
            slot.SlotId,
            slot.ShelfId,
            slot.SlotCode,
            slot.Quantity,
            slot.LastScannedAt,
            slot.ProductSlots.Select(ps => new ProductInSlotDto(
                ps.ProductId,
                ps.Product?.ProductName ?? string.Empty,
                ps.Product?.ImageUrl,
                slot.Quantity > 0 ? slot.Quantity : 0))
                .ToList());
    }
}
