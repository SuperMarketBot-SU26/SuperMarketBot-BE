using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// API quản lý Shelf, Slot và ProductSlot.
/// Cung cấp CRUD đầy đủ cho cấu trúc kệ hàng trong siêu thị.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize(Roles = Roles.AdminOrStaff)]
public sealed class ShelvesController(IShelfService shelfService) : ControllerBase
{
    // ─── Shelf ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách tất cả Shelf, hỗ trợ lọc theo AisleId.
    /// GET /api/v1/shelves?aisleId=1
    /// </summary>
    [HttpGet("shelves")]
    [ProducesResponseType(typeof(IReadOnlyList<ShelfSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShelfSummaryDto>>> GetShelves(
        [FromQuery] int? aisleId,
        CancellationToken cancellationToken)
    {
        var shelves = await shelfService.GetShelvesAsync(aisleId, cancellationToken);
        return Ok(shelves);
    }

    /// <summary>
    /// Lấy chi tiết một Shelf kèm Slots và Products.
    /// GET /api/v1/shelves/{shelfId}
    /// </summary>
    [HttpGet("shelves/{shelfId:int}")]
    [ProducesResponseType(typeof(ShelfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShelfDto>> GetShelfById(int shelfId, CancellationToken cancellationToken)
    {
        var shelf = await shelfService.GetShelfByIdAsync(shelfId, cancellationToken);
        return shelf is null ? NotFound() : Ok(shelf);
    }

    /// <summary>
    /// Tạo Shelf mới với Slots tự động.
    /// POST /api/v1/shelves
    /// </summary>
    [HttpPost("shelves")]
    [ProducesResponseType(typeof(ShelfDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShelfDto>> CreateShelf(
        [FromBody] CreateShelfRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var shelf = await shelfService.CreateShelfAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetShelfById), new { shelfId = shelf.ShelfId }, shelf);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật Shelf.
    /// PUT /api/v1/shelves/{shelfId}
    /// </summary>
    [HttpPut("shelves/{shelfId:int}")]
    [ProducesResponseType(typeof(ShelfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShelfDto>> UpdateShelf(
        int shelfId,
        [FromBody] UpdateShelfRequestDto request,
        CancellationToken cancellationToken)
    {
        var shelf = await shelfService.UpdateShelfAsync(shelfId, request, cancellationToken);
        return shelf is null ? NotFound() : Ok(shelf);
    }

    /// <summary>
    /// Xóa Shelf (cascade xóa Slots).
    /// DELETE /api/v1/shelves/{shelfId}
    /// </summary>
    [HttpDelete("shelves/{shelfId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShelf(int shelfId, CancellationToken cancellationToken)
    {
        var deleted = await shelfService.DeleteShelfAsync(shelfId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // ─── Slot ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy Slot theo ID.
    /// GET /api/v1/slots/{slotId}
    /// </summary>
    [HttpGet("slots/{slotId:int}")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SlotDto>> GetSlotById(int slotId, CancellationToken cancellationToken)
    {
        var slot = await shelfService.GetSlotByIdAsync(slotId, cancellationToken);
        return slot is null ? NotFound() : Ok(slot);
    }

    /// <summary>
    /// Lấy tất cả Slots của một Shelf.
    /// GET /api/v1/shelves/{shelfId}/slots
    /// </summary>
    [HttpGet("shelves/{shelfId:int}/slots")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SlotSummaryDto>>> GetSlotsByShelf(
        int shelfId,
        CancellationToken cancellationToken)
    {
        var slots = await shelfService.GetSlotsByShelfAsync(shelfId, cancellationToken);
        return Ok(slots);
    }

    /// <summary>
    /// Tạo Slot mới trên Shelf.
    /// POST /api/v1/slots
    /// </summary>
    [HttpPost("slots")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SlotDto>> CreateSlot(
        [FromBody] CreateSlotRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var slot = await shelfService.CreateSlotAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetSlotById), new { slotId = slot.SlotId }, slot);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật Slot.
    /// PUT /api/v1/slots/{slotId}
    /// </summary>
    [HttpPut("slots/{slotId:int}")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SlotDto>> UpdateSlot(
        int slotId,
        [FromBody] UpdateSlotRequestDto request,
        CancellationToken cancellationToken)
    {
        var slot = await shelfService.UpdateSlotAsync(slotId, request, cancellationToken);
        return slot is null ? NotFound() : Ok(slot);
    }

    /// <summary>
    /// Xóa Slot.
    /// DELETE /api/v1/slots/{slotId}
    /// </summary>
    [HttpDelete("slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSlot(int slotId, CancellationToken cancellationToken)
    {
        var deleted = await shelfService.DeleteSlotAsync(slotId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // ─── ProductSlot ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gán sản phẩm vào Slot.
    /// POST /api/v1/slots/{slotId}/products
    /// </summary>
    [HttpPost("slots/{slotId:int}/products")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SlotDto>> AssignProductToSlot(
        int slotId,
        [FromBody] AssignProductToSlotRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = new AssignProductToSlotRequestDto(slotId, request.ProductId, request.Quantity);
            var slot = await shelfService.AssignProductToSlotAsync(dto, cancellationToken);
            return slot is null ? NotFound() : Ok(slot);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa sản phẩm khỏi Slot.
    /// DELETE /api/v1/slots/{slotId}/products/{productId}
    /// </summary>
    [HttpDelete("slots/{slotId:int}/products/{productId:int}")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SlotDto>> RemoveProductFromSlot(
        int slotId,
        int productId,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = new RemoveProductFromSlotRequestDto(slotId, productId);
            var slot = await shelfService.RemoveProductFromSlotAsync(dto, cancellationToken);
            return slot is null ? NotFound() : Ok(slot);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm trong Slot.
    /// PATCH /api/v1/slots/{slotId}/products/{productId}/quantity
    /// </summary>
    [HttpPatch("slots/{slotId:int}/products/{productId:int}/quantity")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SlotDto>> SetSlotQuantity(
        int slotId,
        int productId,
        [FromBody] SetSlotQuantityRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = new SetSlotQuantityRequestDto(slotId, productId, request.Quantity);
            var slot = await shelfService.SetSlotQuantityAsync(dto, cancellationToken);
            return slot is null ? NotFound() : Ok(slot);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tìm Slots chứa sản phẩm cụ thể (để tìm vị trí sản phẩm).
    /// GET /api/v1/products/{productId}/slots
    /// </summary>
    [HttpGet("products/{productId:int}/slots")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SlotDto>>> FindSlotsByProduct(
        int productId,
        CancellationToken cancellationToken)
    {
        var slots = await shelfService.FindSlotsByProductAsync(productId, cancellationToken);
        return Ok(slots);
    }
}
