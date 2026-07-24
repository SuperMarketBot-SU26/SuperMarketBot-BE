namespace SmartMarketBot.Application.Models.Maps;

// ─── Floor ──────────────────────────────────────────────────────────────────

/// <summary>
/// Thông tin tóm tắt của 1 tầng trong siêu thị.
/// </summary>
public sealed record FloorDto(
    int FloorId,
    int FloorNumber,
    int ZoneCount,
    int MapCount);

// ─── Zone ───────────────────────────────────────────────────────────────────

/// <summary>
/// Zone (khu vực) trên bản đồ tầng.
/// </summary>
public sealed record ZoneDto(
    int ZoneId,
    int FloorId,
    string? ZoneName,
    string? Description);

/// <summary>
/// Zone chi tiết kèm số lượng Aisles.
/// </summary>
public sealed record ZoneDetailDto(
    int ZoneId,
    int FloorId,
    string? ZoneName,
    string? Description,
    int AisleCount,
    IReadOnlyList<AisleSummaryDto> Aisles);

/// <summary>
/// Zone đơn giản (cho dropdown).
/// </summary>
public sealed record ZoneSummaryDto(
    int ZoneId,
    int FloorId,
    string ZoneName);

// ─── Aisle ──────────────────────────────────────────────────────────────────

/// <summary>
/// Aisle (dãy kệ hàng) thuộc một Zone.
/// </summary>
public sealed record AisleDto(
    int AisleId,
    int ZoneId,
    string AisleCode,
    string? AisleName);

/// <summary>
/// Aisle chi tiết kèm số lượng Shelves.
/// </summary>
public sealed record AisleDetailDto(
    int AisleId,
    int ZoneId,
    string AisleCode,
    string? AisleName,
    string? ZoneName,
    int ShelfCount,
    IReadOnlyList<ShelfSummaryDto> Shelves);

/// <summary>
/// Aisle đơn giản (cho dropdown).
/// </summary>
public sealed record AisleSummaryDto(
    int AisleId,
    int ZoneId,
    string AisleCode,
    string? AisleName);

/// <summary>
/// Mật độ hàng hoá của một kệ sau lần scan gần nhất.
/// </summary>
public sealed record AisleDensityDto(
    int AisleId,
    string AisleCode,
    string? AisleName,
    int? LatestScanId,
    DateTime? ScannedAt,
    decimal DensityPercentage,
    decimal EmptyPercentage,
    bool NeedsRestock,
    string? ImageUrl,
    /// <summary>Màu hiển thị tương ứng với mật độ: green ≥ 70%, yellow 40–69%, red &lt; 40%.</summary>
    string DensityColor);

// ─── Shelf ──────────────────────────────────────────────────────────────────

/// <summary>
/// Shelf (kệ hàng) thuộc một Aisle.
/// </summary>
public sealed record ShelfDto(
    int ShelfId,
    int AisleId,
    int LevelNumber,
    string? AisleCode,
    string? AisleName,
    string? ZoneName,
    IReadOnlyList<SlotDto> Slots);

/// <summary>
/// Shelf đơn giản (không include slots) — dùng cho list/dropdown.
/// </summary>
public sealed record ShelfSummaryDto(
    int ShelfId,
    int AisleId,
    string AisleCode,
    string? AisleName,
    int LevelNumber);

/// <summary>
/// Slot (ô chứa sản phẩm) thuộc một Shelf.
/// </summary>
public sealed record SlotDto(
    int SlotId,
    int ShelfId,
    string? SlotCode,
    int Quantity,
    DateTime? LastScannedAt,
    IReadOnlyList<ProductInSlotDto> Products);

/// <summary>
/// Slot đơn giản (không include products).
/// </summary>
public sealed record SlotSummaryDto(
    int SlotId,
    int ShelfId,
    string? SlotCode,
    int Quantity,
    DateTime? LastScannedAt);

/// <summary>
/// Sản phẩm trong một Slot.
/// </summary>
public sealed record ProductInSlotDto(
    int ProductId,
    string ProductName,
    string? ImageUrl,
    int Quantity);

// ─── Request DTOs ───────────────────────────────────────────────────────────

/// <summary>
/// Tạo Floor mới.
/// </summary>
public sealed record CreateFloorRequestDto(int FloorNumber);

/// <summary>
/// Cập nhật Floor.
/// </summary>
public sealed record UpdateFloorRequestDto(int? FloorNumber);

/// <summary>
/// Tạo Zone mới.
/// </summary>
public sealed record CreateZoneRequestDto(
    int FloorId,
    string? ZoneName,
    string? Description);

/// <summary>
/// Cập nhật Zone.
/// </summary>
public sealed record UpdateZoneRequestDto(
    string? ZoneName,
    string? Description);

/// <summary>
/// Tạo Aisle mới.
/// </summary>
public sealed record CreateAisleRequestDto(
    int ZoneId,
    string AisleCode,
    string? AisleName);

/// <summary>
/// Cập nhật Aisle.
/// </summary>
public sealed record UpdateAisleRequestDto(
    string? AisleCode,
    string? AisleName);

/// <summary>
/// Tạo Shelf mới kèm Slots.
/// </summary>
public sealed record CreateShelfRequestDto(
    int AisleId,
    int LevelNumber,
    int SlotCount,
    string? ShelfLabel);

/// <summary>
/// Cập nhật Shelf.
/// </summary>
public sealed record UpdateShelfRequestDto(
    int? LevelNumber,
    string? ShelfLabel);

/// <summary>
/// Tạo Slot mới trên Shelf.
/// </summary>
public sealed record CreateSlotRequestDto(
    int ShelfId,
    string? SlotCode);

/// <summary>
/// Cập nhật Slot.
/// </summary>
public sealed record UpdateSlotRequestDto(
    string? SlotCode,
    int? Quantity);

/// <summary>
/// Gán sản phẩm vào Slot.
/// </summary>
public sealed record AssignProductToSlotRequestDto(
    int SlotId,
    int ProductId,
    int Quantity = 1);

/// <summary>
/// Xóa sản phẩm khỏi Slot.
/// </summary>
public sealed record RemoveProductFromSlotRequestDto(
    int SlotId,
    int ProductId);

/// <summary>
/// Đặt số lượng sản phẩm trong Slot.
/// </summary>
public sealed record SetSlotQuantityRequestDto(
    int SlotId,
    int ProductId,
    int Quantity);
