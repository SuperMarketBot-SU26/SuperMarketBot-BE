using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class ZoneAisleService(AppDbContext db) : IZoneAisleService
{
    // ─── Floor ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FloorDto>> GetFloorsAsync(CancellationToken ct = default)
    {
        return await db.Floors
            .AsNoTracking()
            .Include(f => f.Zones)
            .Include(f => f.Maps)
            .OrderBy(f => f.FloorNumber)
            .Select(f => new FloorDto(f.FloorId, f.FloorNumber, f.Zones.Count, f.Maps.Count))
            .ToListAsync(ct);
    }

    public async Task<FloorDto?> GetFloorByIdAsync(int floorId, CancellationToken ct = default)
    {
        return await db.Floors
            .AsNoTracking()
            .Include(f => f.Zones)
            .Include(f => f.Maps)
            .Where(f => f.FloorId == floorId)
            .Select(f => new FloorDto(f.FloorId, f.FloorNumber, f.Zones.Count, f.Maps.Count))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FloorDto> CreateFloorAsync(CreateFloorRequestDto request, CancellationToken ct = default)
    {
        var floor = new Floor { FloorNumber = request.FloorNumber };
        db.Floors.Add(floor);
        await db.SaveChangesAsync(ct);

        return new FloorDto(floor.FloorId, floor.FloorNumber, 0, 0);
    }

    public async Task<FloorDto?> UpdateFloorAsync(int floorId, UpdateFloorRequestDto request, CancellationToken ct = default)
    {
        var floor = await db.Floors
            .Include(f => f.Zones)
            .Include(f => f.Maps)
            .FirstOrDefaultAsync(f => f.FloorId == floorId, ct);

        if (floor is null) return null;

        if (request.FloorNumber.HasValue)
            floor.FloorNumber = request.FloorNumber.Value;

        await db.SaveChangesAsync(ct);
        return new FloorDto(floor.FloorId, floor.FloorNumber, floor.Zones.Count, floor.Maps.Count);
    }

    public async Task<bool> DeleteFloorAsync(int floorId, CancellationToken ct = default)
    {
        var floor = await db.Floors.FindAsync(new object[] { floorId }, ct);
        if (floor is null) return false;

        db.Floors.Remove(floor);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ─── Zone ────────────────────────────────────────────────────────────────

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

    public async Task<ZoneDetailDto?> GetZoneByIdAsync(int zoneId, CancellationToken ct = default)
    {
        var zone = await db.Zones
            .AsNoTracking()
            .Include(z => z.Aisles)
            .FirstOrDefaultAsync(z => z.ZoneId == zoneId, ct);

        if (zone is null) return null;

        return new ZoneDetailDto(
            zone.ZoneId,
            zone.FloorId,
            zone.ZoneName,
            zone.Description,
            zone.Aisles.Count,
            zone.Aisles.Select(a => new AisleSummaryDto(a.AisleId, a.ZoneId, a.AisleCode, a.AisleName)).ToList());
    }

    public async Task<ZoneDto> CreateZoneAsync(CreateZoneRequestDto request, CancellationToken ct = default)
    {
        var floorExists = await db.Floors.AnyAsync(f => f.FloorId == request.FloorId, ct);
        if (!floorExists)
            throw new KeyNotFoundException($"Floor '{request.FloorId}' not found.");

        var zone = new Zone
        {
            FloorId = request.FloorId,
            ZoneName = request.ZoneName,
            Description = request.Description
        };

        db.Zones.Add(zone);
        await db.SaveChangesAsync(ct);

        return new ZoneDto(zone.ZoneId, zone.FloorId, zone.ZoneName, zone.Description);
    }

    public async Task<ZoneDto?> UpdateZoneAsync(int zoneId, UpdateZoneRequestDto request, CancellationToken ct = default)
    {
        var zone = await db.Zones.FindAsync(new object[] { zoneId }, ct);
        if (zone is null) return null;

        if (request.ZoneName is not null)
            zone.ZoneName = request.ZoneName;

        if (request.Description is not null)
            zone.Description = request.Description;

        await db.SaveChangesAsync(ct);
        return new ZoneDto(zone.ZoneId, zone.FloorId, zone.ZoneName, zone.Description);
    }

    public async Task<bool> DeleteZoneAsync(int zoneId, CancellationToken ct = default)
    {
        var zone = await db.Zones.FindAsync(new object[] { zoneId }, ct);
        if (zone is null) return false;

        db.Zones.Remove(zone);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ─── Aisle ──────────────────────────────────────────────────────────────

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

    public async Task<AisleDetailDto?> GetAisleByIdAsync(int aisleId, CancellationToken ct = default)
    {
        var aisle = await db.Aisles
            .AsNoTracking()
            .Include(a => a.Zone)
            .Include(a => a.Shelves)
            .FirstOrDefaultAsync(a => a.AisleId == aisleId, ct);

        if (aisle is null) return null;

        return new AisleDetailDto(
            aisle.AisleId,
            aisle.ZoneId,
            aisle.AisleCode,
            aisle.AisleName,
            aisle.Zone?.ZoneName,
            aisle.Shelves.Count,
            aisle.Shelves.Select(s => new ShelfSummaryDto(s.ShelfId, s.AisleId, aisle.AisleCode, aisle.AisleName, s.LevelNumber)).ToList());
    }

    public async Task<AisleDto> CreateAisleAsync(CreateAisleRequestDto request, CancellationToken ct = default)
    {
        var zoneExists = await db.Zones.AnyAsync(z => z.ZoneId == request.ZoneId, ct);
        if (!zoneExists)
            throw new KeyNotFoundException($"Zone '{request.ZoneId}' not found.");

        var aisle = new Aisle
        {
            ZoneId = request.ZoneId,
            AisleCode = request.AisleCode,
            AisleName = request.AisleName
        };

        db.Aisles.Add(aisle);
        await db.SaveChangesAsync(ct);

        return new AisleDto(aisle.AisleId, aisle.ZoneId, aisle.AisleCode, aisle.AisleName);
    }

    public async Task<AisleDto?> UpdateAisleAsync(int aisleId, UpdateAisleRequestDto request, CancellationToken ct = default)
    {
        var aisle = await db.Aisles.FindAsync(new object[] { aisleId }, ct);
        if (aisle is null) return null;

        if (!string.IsNullOrWhiteSpace(request.AisleCode))
            aisle.AisleCode = request.AisleCode;

        if (request.AisleName is not null)
            aisle.AisleName = request.AisleName;

        await db.SaveChangesAsync(ct);
        return new AisleDto(aisle.AisleId, aisle.ZoneId, aisle.AisleCode, aisle.AisleName);
    }

    public async Task<bool> DeleteAisleAsync(int aisleId, CancellationToken ct = default)
    {
        var aisle = await db.Aisles.FindAsync(new object[] { aisleId }, ct);
        if (aisle is null) return false;

        db.Aisles.Remove(aisle);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ─── Shelf ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ShelfSummaryDto>> GetShelvesAsync(int? aisleId = null, CancellationToken ct = default)
    {
        var query = db.Shelves.AsNoTracking().Include(s => s.Aisle).AsQueryable();

        if (aisleId.HasValue)
            query = query.Where(s => s.AisleId == aisleId.Value);

        var shelves = await query.OrderBy(s => s.Aisle!.AisleCode).ThenBy(s => s.LevelNumber).ToListAsync(ct);

        return shelves.Select(s => new ShelfSummaryDto(
            s.ShelfId, s.AisleId, s.Aisle!.AisleCode, s.Aisle.AisleName, s.LevelNumber)).ToList();
    }

    public async Task<ShelfDto?> GetShelfByIdAsync(int shelfId, CancellationToken ct = default)
    {
        var shelf = await db.Shelves
            .AsNoTracking()
            .Include(s => s.Aisle).ThenInclude(a => a!.Zone)
            .Include(s => s.Slots).ThenInclude(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(s => s.ShelfId == shelfId, ct);

        if (shelf is null) return null;

        return ToShelfDto(shelf);
    }

    public async Task<ShelfDto> CreateShelfAsync(CreateShelfRequestDto request, CancellationToken ct = default)
    {
        var aisleExists = await db.Aisles.AnyAsync(a => a.AisleId == request.AisleId, ct);
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
                SlotCode = string.IsNullOrWhiteSpace(request.ShelfLabel) ? $"S{i}" : $"{request.ShelfLabel}-{i}",
                Quantity = 0
            });
        }

        db.Shelves.Add(shelf);
        await db.SaveChangesAsync(ct);

        var created = await db.Shelves
            .Include(s => s.Aisle).ThenInclude(a => a!.Zone)
            .Include(s => s.Slots).ThenInclude(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .FirstAsync(s => s.ShelfId == shelf.ShelfId, ct);

        return ToShelfDto(created);
    }

    public async Task<ShelfDto?> UpdateShelfAsync(int shelfId, UpdateShelfRequestDto request, CancellationToken ct = default)
    {
        var shelf = await db.Shelves
            .Include(s => s.Aisle).ThenInclude(a => a!.Zone)
            .Include(s => s.Slots).ThenInclude(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(s => s.ShelfId == shelfId, ct);

        if (shelf is null) return null;

        if (request.LevelNumber.HasValue)
            shelf.LevelNumber = request.LevelNumber.Value;

        await db.SaveChangesAsync(ct);
        return ToShelfDto(shelf);
    }

    public async Task<bool> DeleteShelfAsync(int shelfId, CancellationToken ct = default)
    {
        var shelf = await db.Shelves.FindAsync(new object[] { shelfId }, ct);
        if (shelf is null) return false;

        db.Shelves.Remove(shelf);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ─── Slot ──────────────────────────────────────────────────────────────

    public async Task<SlotDto?> GetSlotByIdAsync(int slotId, CancellationToken ct = default)
    {
        var slot = await db.Slots
            .AsNoTracking()
            .Include(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(sl => sl.SlotId == slotId, ct);

        return slot is null ? null : ToSlotDto(slot);
    }

    public async Task<IReadOnlyList<SlotSummaryDto>> GetSlotsByShelfAsync(int shelfId, CancellationToken ct = default)
    {
        return await db.Slots
            .AsNoTracking()
            .Where(sl => sl.ShelfId == shelfId)
            .OrderBy(sl => sl.SlotCode)
            .Select(sl => new SlotSummaryDto(sl.SlotId, sl.ShelfId, sl.SlotCode, sl.Quantity, sl.LastScannedAt))
            .ToListAsync(ct);
    }

    public async Task<SlotDto> CreateSlotAsync(CreateSlotRequestDto request, CancellationToken ct = default)
    {
        var shelfExists = await db.Shelves.AnyAsync(s => s.ShelfId == request.ShelfId, ct);
        if (!shelfExists)
            throw new KeyNotFoundException($"Shelf '{request.ShelfId}' not found.");

        var slot = new Slot { ShelfId = request.ShelfId, SlotCode = request.SlotCode, Quantity = 0 };
        db.Slots.Add(slot);
        await db.SaveChangesAsync(ct);

        var created = await db.Slots
            .Include(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .FirstAsync(sl => sl.SlotId == slot.SlotId, ct);

        return ToSlotDto(created);
    }

    public async Task<SlotDto?> UpdateSlotAsync(int slotId, UpdateSlotRequestDto request, CancellationToken ct = default)
    {
        var slot = await db.Slots
            .Include(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(sl => sl.SlotId == slotId, ct);

        if (slot is null) return null;

        if (!string.IsNullOrWhiteSpace(request.SlotCode))
            slot.SlotCode = request.SlotCode;

        if (request.Quantity.HasValue)
            slot.Quantity = request.Quantity.Value;

        await db.SaveChangesAsync(ct);
        return ToSlotDto(slot);
    }

    public async Task<bool> DeleteSlotAsync(int slotId, CancellationToken ct = default)
    {
        var slot = await db.Slots.FindAsync(new object[] { slotId }, ct);
        if (slot is null) return false;

        db.Slots.Remove(slot);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ─── ProductSlot ───────────────────────────────────────────────────────

    public async Task<SlotDto?> AssignProductToSlotAsync(AssignProductToSlotRequestDto request, CancellationToken ct = default)
    {
        var slotExists = await db.Slots.AnyAsync(s => s.SlotId == request.SlotId, ct);
        if (!slotExists)
            throw new KeyNotFoundException($"Slot '{request.SlotId}' not found.");

        var productExists = await db.Products.AnyAsync(p => p.ProductId == request.ProductId, ct);
        if (!productExists)
            throw new KeyNotFoundException($"Product '{request.ProductId}' not found.");

        var existing = await db.ProductSlots
            .FirstOrDefaultAsync(ps => ps.SlotId == request.SlotId && ps.ProductId == request.ProductId, ct);

        if (existing is null)
        {
            db.ProductSlots.Add(new ProductSlot { SlotId = request.SlotId, ProductId = request.ProductId });
        }

        var slot = await db.Slots.FindAsync(new object[] { request.SlotId }, ct);
        if (slot is not null)
        {
            slot.Quantity = request.Quantity;
            await db.SaveChangesAsync(ct);
        }

        var updated = await db.Slots
            .Include(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .FirstAsync(sl => sl.SlotId == request.SlotId, ct);

        return ToSlotDto(updated);
    }

    public async Task<SlotDto?> RemoveProductFromSlotAsync(RemoveProductFromSlotRequestDto request, CancellationToken ct = default)
    {
        var productSlot = await db.ProductSlots
            .FirstOrDefaultAsync(ps => ps.SlotId == request.SlotId && ps.ProductId == request.ProductId, ct);

        if (productSlot is null)
            throw new KeyNotFoundException($"ProductSlot not found for Slot '{request.SlotId}' and Product '{request.ProductId}'.");

        db.ProductSlots.Remove(productSlot);

        var slot = await db.Slots.FindAsync(new object[] { request.SlotId }, ct);
        if (slot is not null && slot.Quantity > 0)
            slot.Quantity--;

        await db.SaveChangesAsync(ct);

        var updated = await db.Slots
            .Include(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .FirstOrDefaultAsync(sl => sl.SlotId == request.SlotId, ct);

        return updated is null ? null : ToSlotDto(updated);
    }

    public async Task<SlotDto?> SetSlotQuantityAsync(SetSlotQuantityRequestDto request, CancellationToken ct = default)
    {
        var slot = await db.Slots.FindAsync(new object[] { request.SlotId }, ct);
        if (slot is null) return null;

        var productSlot = await db.ProductSlots
            .FirstOrDefaultAsync(ps => ps.SlotId == request.SlotId && ps.ProductId == request.ProductId, ct);

        if (productSlot is null)
        {
            var productExists = await db.Products.AnyAsync(p => p.ProductId == request.ProductId, ct);
            if (!productExists)
                throw new KeyNotFoundException($"Product '{request.ProductId}' not found.");

            db.ProductSlots.Add(new ProductSlot { SlotId = request.SlotId, ProductId = request.ProductId });
        }

        slot.Quantity = request.Quantity;
        await db.SaveChangesAsync(ct);

        var updated = await db.Slots
            .Include(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .FirstAsync(sl => sl.SlotId == request.SlotId, ct);

        return ToSlotDto(updated);
    }

    public async Task<IReadOnlyList<SlotDto>> FindSlotsByProductAsync(int productId, CancellationToken ct = default)
    {
        var slots = await db.Slots
            .AsNoTracking()
            .Include(sl => sl.ProductSlots).ThenInclude(ps => ps.Product)
            .Include(sl => sl.Shelf).ThenInclude(s => s!.Aisle)
            .Where(sl => sl.ProductSlots.Any(ps => ps.ProductId == productId))
            .ToListAsync(ct);

        return slots.Select(ToSlotDto).ToList();
    }

    // ─── Aisle Density ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AisleDensityDto>> GetAisleDensitiesAsync(int? zoneId = null, CancellationToken ct = default)
    {
        var query = db.Aisles.AsNoTracking();
        if (zoneId.HasValue)
            query = query.Where(a => a.ZoneId == zoneId.Value);

        var aisles = await query.OrderBy(a => a.AisleCode).ToListAsync(ct);
        if (aisles.Count == 0)
            return [];

        var aisleIds = aisles.Select(a => a.AisleId).ToList();

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
            var empty = scan?.EmptyPercentage ?? 0m;
            var color = density >= 70m ? "green" : density >= 40m ? "yellow" : "red";

            return new AisleDensityDto(
                a.AisleId, a.AisleCode, a.AisleName,
                scan?.ScanId, scan?.ScannedAt, density, empty,
                scan?.NeedsRestock ?? false, scan?.ImageUrl, color);
        }).ToList();
    }

    // ─── Private Helpers ───────────────────────────────────────────────────

    private static ShelfDto ToShelfDto(Shelf shelf)
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
                slot.Quantity > 0 ? slot.Quantity : 0)).ToList());
    }
}
