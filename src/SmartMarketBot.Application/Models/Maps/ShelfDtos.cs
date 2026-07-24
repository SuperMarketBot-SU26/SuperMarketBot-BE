namespace SmartMarketBot.Application.Models.Maps;

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
