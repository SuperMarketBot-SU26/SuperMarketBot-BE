using SmartMarketBot.Application.Models.Maps;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// Service quản lý Floor, Zone, Aisle, Shelf, Slot theo chain:
/// Floor → Zone → Aisle → Shelf → Slot → ProductSlot
/// </summary>
public interface IZoneAisleService
{
    // ─── Floor ──────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Floor.</summary>
    Task<IReadOnlyList<FloorDto>> GetFloorsAsync(CancellationToken ct = default);

    /// <summary>Lấy Floor theo ID.</summary>
    Task<FloorDto?> GetFloorByIdAsync(int floorId, CancellationToken ct = default);

    /// <summary>Tạo Floor mới.</summary>
    Task<FloorDto> CreateFloorAsync(CreateFloorRequestDto request, CancellationToken ct = default);

    /// <summary>Cập nhật Floor.</summary>
    Task<FloorDto?> UpdateFloorAsync(int floorId, UpdateFloorRequestDto request, CancellationToken ct = default);

    /// <summary>Xóa Floor.</summary>
    Task<bool> DeleteFloorAsync(int floorId, CancellationToken ct = default);

    // ─── Zone ────────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Zone. Hỗ trợ lọc theo floorId.</summary>
    Task<IReadOnlyList<ZoneDto>> GetZonesAsync(int? floorId = null, CancellationToken ct = default);

    /// <summary>Lấy Zone theo ID kèm danh sách Aisles.</summary>
    Task<ZoneDetailDto?> GetZoneByIdAsync(int zoneId, CancellationToken ct = default);

    /// <summary>Tạo Zone mới.</summary>
    Task<ZoneDto> CreateZoneAsync(CreateZoneRequestDto request, CancellationToken ct = default);

    /// <summary>Cập nhật Zone.</summary>
    Task<ZoneDto?> UpdateZoneAsync(int zoneId, UpdateZoneRequestDto request, CancellationToken ct = default);

    /// <summary>Xóa Zone (cascade xóa Aisles, Shelves, Slots).</summary>
    Task<bool> DeleteZoneAsync(int zoneId, CancellationToken ct = default);

    // ─── Aisle ──────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Aisle. Hỗ trợ lọc theo zoneId.</summary>
    Task<IReadOnlyList<AisleDto>> GetAislesAsync(int? zoneId = null, CancellationToken ct = default);

    /// <summary>Lấy Aisle theo ID kèm danh sách Shelves.</summary>
    Task<AisleDetailDto?> GetAisleByIdAsync(int aisleId, CancellationToken ct = default);

    /// <summary>Tạo Aisle mới.</summary>
    Task<AisleDto> CreateAisleAsync(CreateAisleRequestDto request, CancellationToken ct = default);

    /// <summary>Cập nhật Aisle.</summary>
    Task<AisleDto?> UpdateAisleAsync(int aisleId, UpdateAisleRequestDto request, CancellationToken ct = default);

    /// <summary>Xóa Aisle (cascade xóa Shelves, Slots).</summary>
    Task<bool> DeleteAisleAsync(int aisleId, CancellationToken ct = default);

    // ─── Shelf ──────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Shelf. Hỗ trợ lọc theo aisleId.</summary>
    Task<IReadOnlyList<ShelfSummaryDto>> GetShelvesAsync(int? aisleId = null, CancellationToken ct = default);

    /// <summary>Lấy Shelf theo ID kèm Slots và Products.</summary>
    Task<ShelfDto?> GetShelfByIdAsync(int shelfId, CancellationToken ct = default);

    /// <summary>Tạo Shelf mới với Slots tự động.</summary>
    Task<ShelfDto> CreateShelfAsync(CreateShelfRequestDto request, CancellationToken ct = default);

    /// <summary>Cập nhật Shelf.</summary>
    Task<ShelfDto?> UpdateShelfAsync(int shelfId, UpdateShelfRequestDto request, CancellationToken ct = default);

    /// <summary>Xóa Shelf (cascade xóa Slots).</summary>
    Task<bool> DeleteShelfAsync(int shelfId, CancellationToken ct = default);

    // ─── Slot ──────────────────────────────────────────────────────────────

    /// <summary>Lấy Slot theo ID.</summary>
    Task<SlotDto?> GetSlotByIdAsync(int slotId, CancellationToken ct = default);

    /// <summary>Lấy tất cả Slots của một Shelf.</summary>
    Task<IReadOnlyList<SlotSummaryDto>> GetSlotsByShelfAsync(int shelfId, CancellationToken ct = default);

    /// <summary>Tạo Slot mới.</summary>
    Task<SlotDto> CreateSlotAsync(CreateSlotRequestDto request, CancellationToken ct = default);

    /// <summary>Cập nhật Slot.</summary>
    Task<SlotDto?> UpdateSlotAsync(int slotId, UpdateSlotRequestDto request, CancellationToken ct = default);

    /// <summary>Xóa Slot.</summary>
    Task<bool> DeleteSlotAsync(int slotId, CancellationToken ct = default);

    // ─── ProductSlot ───────────────────────────────────────────────────────

    /// <summary>Gán sản phẩm vào Slot.</summary>
    Task<SlotDto?> AssignProductToSlotAsync(AssignProductToSlotRequestDto request, CancellationToken ct = default);

    /// <summary>Xóa sản phẩm khỏi Slot.</summary>
    Task<SlotDto?> RemoveProductFromSlotAsync(RemoveProductFromSlotRequestDto request, CancellationToken ct = default);

    /// <summary>Cập nhật số lượng sản phẩm trong Slot.</summary>
    Task<SlotDto?> SetSlotQuantityAsync(SetSlotQuantityRequestDto request, CancellationToken ct = default);

    /// <summary>Tìm Slots chứa sản phẩm cụ thể.</summary>
    Task<IReadOnlyList<SlotDto>> FindSlotsByProductAsync(int productId, CancellationToken ct = default);

    // ─── Aisle Density ─────────────────────────────────────────────────────

    /// <summary>
    /// Mật độ hàng hoá của từng kệ (Aisle) dựa trên lần AisleScan gần nhất.
    /// Trả kèm DensityColor (green/yellow/red) để frontend tô màu trực tiếp.
    /// Hỗ trợ lọc theo zoneId.
    /// </summary>
    Task<IReadOnlyList<AisleDensityDto>> GetAisleDensitiesAsync(int? zoneId = null, CancellationToken ct = default);
}
