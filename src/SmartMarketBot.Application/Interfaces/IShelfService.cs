using SmartMarketBot.Application.Models.Maps;

namespace SmartMarketBot.Application.Interfaces;

public interface IShelfService
{
    // ─── Shelf ──────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Shelf, có filter theo AisleId.</summary>
    Task<IReadOnlyList<ShelfSummaryDto>> GetShelvesAsync(int? aisleId, CancellationToken cancellationToken = default);

    /// <summary>Lấy chi tiết một Shelf kèm Slots và Products.</summary>
    Task<ShelfDto?> GetShelfByIdAsync(int shelfId, CancellationToken cancellationToken = default);

    /// <summary>Tạo Shelf mới với Slots tự động.</summary>
    Task<ShelfDto> CreateShelfAsync(CreateShelfRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật Shelf.</summary>
    Task<ShelfDto?> UpdateShelfAsync(int shelfId, UpdateShelfRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Xóa Shelf (cascade xóa Slots).</summary>
    Task<bool> DeleteShelfAsync(int shelfId, CancellationToken cancellationToken = default);

    // ─── Slot ────────────────────────────────────────────────────────────────

    /// <summary>Lấy Slot theo ID.</summary>
    Task<SlotDto?> GetSlotByIdAsync(int slotId, CancellationToken cancellationToken = default);

    /// <summary>Tạo Slot mới trên Shelf.</summary>
    Task<SlotDto> CreateSlotAsync(CreateSlotRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật Slot.</summary>
    Task<SlotDto?> UpdateSlotAsync(int slotId, UpdateSlotRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Xóa Slot.</summary>
    Task<bool> DeleteSlotAsync(int slotId, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật số lượng sản phẩm trong Slot.</summary>
    Task<SlotDto?> SetSlotQuantityAsync(SetSlotQuantityRequestDto request, CancellationToken cancellationToken = default);

    // ─── ProductSlot ─────────────────────────────────────────────────────────

    /// <summary>Gán sản phẩm vào Slot.</summary>
    Task<SlotDto?> AssignProductToSlotAsync(AssignProductToSlotRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Xóa sản phẩm khỏi Slot.</summary>
    Task<SlotDto?> RemoveProductFromSlotAsync(RemoveProductFromSlotRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Lấy tất cả Slots của một Shelf.</summary>
    Task<IReadOnlyList<SlotSummaryDto>> GetSlotsByShelfAsync(int shelfId, CancellationToken cancellationToken = default);

    /// <summary>Lấy Slots chứa sản phẩm cụ thể (để tìm vị trí sản phẩm).</summary>
    Task<IReadOnlyList<SlotDto>> FindSlotsByProductAsync(int productId, CancellationToken cancellationToken = default);
}
