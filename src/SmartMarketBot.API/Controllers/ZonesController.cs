using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// API quản lý Floor → Zone → Aisle → Shelf → Slot → ProductSlot.
/// Chain đầy đủ: Tầng → Khu vực → Dãy kệ → Kệ → Ô chứa → Sản phẩm.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize(Roles = Roles.AdminOrStaff)]
public sealed class ZonesController(IZoneAisleService zoneAisleService) : ControllerBase
{
    // ─── Floor ──────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Floor.</summary>
    [HttpGet("floors")]
    [ProducesResponseType(typeof(IReadOnlyList<FloorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FloorDto>>> GetFloors(CancellationToken ct)
    {
        var floors = await zoneAisleService.GetFloorsAsync(ct);
        return Ok(floors);
    }

    /// <summary>Lấy Floor theo ID.</summary>
    [HttpGet("floors/{floorId:int}")]
    [ProducesResponseType(typeof(FloorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FloorDto>> GetFloorById(int floorId, CancellationToken ct)
    {
        var floor = await zoneAisleService.GetFloorByIdAsync(floorId, ct);
        return floor is null ? NotFound() : Ok(floor);
    }

    /// <summary>Tạo Floor mới.</summary>
    [HttpPost("floors")]
    [ProducesResponseType(typeof(FloorDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<FloorDto>> CreateFloor([FromBody] CreateFloorRequestDto request, CancellationToken ct)
    {
        var floor = await zoneAisleService.CreateFloorAsync(request, ct);
        return CreatedAtAction(nameof(GetFloorById), new { floorId = floor.FloorId }, floor);
    }

    /// <summary>Cập nhật Floor.</summary>
    [HttpPut("floors/{floorId:int}")]
    [ProducesResponseType(typeof(FloorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FloorDto>> UpdateFloor(int floorId, [FromBody] UpdateFloorRequestDto request, CancellationToken ct)
    {
        var floor = await zoneAisleService.UpdateFloorAsync(floorId, request, ct);
        return floor is null ? NotFound() : Ok(floor);
    }

    /// <summary>Xóa Floor.</summary>
    [HttpDelete("floors/{floorId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFloor(int floorId, CancellationToken ct)
    {
        var deleted = await zoneAisleService.DeleteFloorAsync(floorId, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ─── Zone ────────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Zone. Hỗ trợ lọc theo floorId.</summary>
    [HttpGet("zones")]
    [ProducesResponseType(typeof(IReadOnlyList<ZoneDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ZoneDto>>> GetZones([FromQuery] int? floorId, CancellationToken ct)
    {
        var zones = await zoneAisleService.GetZonesAsync(floorId, ct);
        return Ok(zones);
    }

    /// <summary>Lấy Zone theo ID kèm danh sách Aisles.</summary>
    [HttpGet("zones/{zoneId:int}")]
    [ProducesResponseType(typeof(ZoneDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ZoneDetailDto>> GetZoneById(int zoneId, CancellationToken ct)
    {
        var zone = await zoneAisleService.GetZoneByIdAsync(zoneId, ct);
        return zone is null ? NotFound() : Ok(zone);
    }

    /// <summary>Tạo Zone mới.</summary>
    [HttpPost("zones")]
    [ProducesResponseType(typeof(ZoneDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ZoneDto>> CreateZone([FromBody] CreateZoneRequestDto request, CancellationToken ct)
    {
        try
        {
            var zone = await zoneAisleService.CreateZoneAsync(request, ct);
            return CreatedAtAction(nameof(GetZoneById), new { zoneId = zone.ZoneId }, zone);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Cập nhật Zone.</summary>
    [HttpPut("zones/{zoneId:int}")]
    [ProducesResponseType(typeof(ZoneDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ZoneDto>> UpdateZone(int zoneId, [FromBody] UpdateZoneRequestDto request, CancellationToken ct)
    {
        var zone = await zoneAisleService.UpdateZoneAsync(zoneId, request, ct);
        return zone is null ? NotFound() : Ok(zone);
    }

    /// <summary>Xóa Zone.</summary>
    [HttpDelete("zones/{zoneId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteZone(int zoneId, CancellationToken ct)
    {
        var deleted = await zoneAisleService.DeleteZoneAsync(zoneId, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ─── Aisle ──────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Aisle. Hỗ trợ lọc theo zoneId.</summary>
    [HttpGet("aisles")]
    [ProducesResponseType(typeof(IReadOnlyList<AisleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AisleDto>>> GetAisles([FromQuery] int? zoneId, CancellationToken ct)
    {
        var aisles = await zoneAisleService.GetAislesAsync(zoneId, ct);
        return Ok(aisles);
    }

    /// <summary>Lấy Aisle theo ID kèm danh sách Shelves.</summary>
    [HttpGet("aisles/{aisleId:int}")]
    [ProducesResponseType(typeof(AisleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AisleDetailDto>> GetAisleById(int aisleId, CancellationToken ct)
    {
        var aisle = await zoneAisleService.GetAisleByIdAsync(aisleId, ct);
        return aisle is null ? NotFound() : Ok(aisle);
    }

    /// <summary>Tạo Aisle mới.</summary>
    [HttpPost("aisles")]
    [ProducesResponseType(typeof(AisleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AisleDto>> CreateAisle([FromBody] CreateAisleRequestDto request, CancellationToken ct)
    {
        try
        {
            var aisle = await zoneAisleService.CreateAisleAsync(request, ct);
            return CreatedAtAction(nameof(GetAisleById), new { aisleId = aisle.AisleId }, aisle);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Cập nhật Aisle.</summary>
    [HttpPut("aisles/{aisleId:int}")]
    [ProducesResponseType(typeof(AisleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AisleDto>> UpdateAisle(int aisleId, [FromBody] UpdateAisleRequestDto request, CancellationToken ct)
    {
        var aisle = await zoneAisleService.UpdateAisleAsync(aisleId, request, ct);
        return aisle is null ? NotFound() : Ok(aisle);
    }

    /// <summary>Xóa Aisle.</summary>
    [HttpDelete("aisles/{aisleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAisle(int aisleId, CancellationToken ct)
    {
        var deleted = await zoneAisleService.DeleteAisleAsync(aisleId, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ─── Shelf ──────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Shelf. Hỗ trợ lọc theo aisleId.</summary>
    [HttpGet("shelves")]
    [ProducesResponseType(typeof(IReadOnlyList<ShelfSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShelfSummaryDto>>> GetShelves([FromQuery] int? aisleId, CancellationToken ct)
    {
        var shelves = await zoneAisleService.GetShelvesAsync(aisleId, ct);
        return Ok(shelves);
    }

    /// <summary>Lấy Shelf theo ID kèm Slots và Products.</summary>
    [HttpGet("shelves/{shelfId:int}")]
    [ProducesResponseType(typeof(ShelfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShelfDto>> GetShelfById(int shelfId, CancellationToken ct)
    {
        var shelf = await zoneAisleService.GetShelfByIdAsync(shelfId, ct);
        return shelf is null ? NotFound() : Ok(shelf);
    }

    /// <summary>Tạo Shelf mới với Slots tự động.</summary>
    [HttpPost("shelves")]
    [ProducesResponseType(typeof(ShelfDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShelfDto>> CreateShelf([FromBody] CreateShelfRequestDto request, CancellationToken ct)
    {
        try
        {
            var shelf = await zoneAisleService.CreateShelfAsync(request, ct);
            return CreatedAtAction(nameof(GetShelfById), new { shelfId = shelf.ShelfId }, shelf);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Cập nhật Shelf.</summary>
    [HttpPut("shelves/{shelfId:int}")]
    [ProducesResponseType(typeof(ShelfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShelfDto>> UpdateShelf(int shelfId, [FromBody] UpdateShelfRequestDto request, CancellationToken ct)
    {
        var shelf = await zoneAisleService.UpdateShelfAsync(shelfId, request, ct);
        return shelf is null ? NotFound() : Ok(shelf);
    }

    /// <summary>Xóa Shelf.</summary>
    [HttpDelete("shelves/{shelfId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShelf(int shelfId, CancellationToken ct)
    {
        var deleted = await zoneAisleService.DeleteShelfAsync(shelfId, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ─── Slot ──────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả Slots của một Shelf.</summary>
    [HttpGet("shelves/{shelfId:int}/slots")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SlotSummaryDto>>> GetSlotsByShelf(int shelfId, CancellationToken ct)
    {
        var slots = await zoneAisleService.GetSlotsByShelfAsync(shelfId, ct);
        return Ok(slots);
    }

    /// <summary>Lấy Slot theo ID.</summary>
    [HttpGet("slots/{slotId:int}")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SlotDto>> GetSlotById(int slotId, CancellationToken ct)
    {
        var slot = await zoneAisleService.GetSlotByIdAsync(slotId, ct);
        return slot is null ? NotFound() : Ok(slot);
    }

    /// <summary>Tạo Slot mới.</summary>
    [HttpPost("slots")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SlotDto>> CreateSlot([FromBody] CreateSlotRequestDto request, CancellationToken ct)
    {
        try
        {
            var slot = await zoneAisleService.CreateSlotAsync(request, ct);
            return CreatedAtAction(nameof(GetSlotById), new { slotId = slot.SlotId }, slot);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Cập nhật Slot.</summary>
    [HttpPut("slots/{slotId:int}")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SlotDto>> UpdateSlot(int slotId, [FromBody] UpdateSlotRequestDto request, CancellationToken ct)
    {
        var slot = await zoneAisleService.UpdateSlotAsync(slotId, request, ct);
        return slot is null ? NotFound() : Ok(slot);
    }

    /// <summary>Xóa Slot.</summary>
    [HttpDelete("slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSlot(int slotId, CancellationToken ct)
    {
        var deleted = await zoneAisleService.DeleteSlotAsync(slotId, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ─── ProductSlot ───────────────────────────────────────────────────────

    /// <summary>Gán sản phẩm vào Slot.</summary>
    [HttpPost("slots/{slotId:int}/products")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SlotDto>> AssignProductToSlot(int slotId, [FromBody] AssignProductToSlotRequestDto request, CancellationToken ct)
    {
        try
        {
            var dto = new AssignProductToSlotRequestDto(slotId, request.ProductId, request.Quantity);
            var slot = await zoneAisleService.AssignProductToSlotAsync(dto, ct);
            return slot is null ? NotFound() : Ok(slot);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Xóa sản phẩm khỏi Slot.</summary>
    [HttpDelete("slots/{slotId:int}/products/{productId:int}")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SlotDto>> RemoveProductFromSlot(int slotId, int productId, CancellationToken ct)
    {
        try
        {
            var dto = new RemoveProductFromSlotRequestDto(slotId, productId);
            var slot = await zoneAisleService.RemoveProductFromSlotAsync(dto, ct);
            return slot is null ? NotFound() : Ok(slot);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Cập nhật số lượng sản phẩm trong Slot.</summary>
    [HttpPatch("slots/{slotId:int}/products/{productId:int}/quantity")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SlotDto>> SetSlotQuantity(int slotId, int productId, [FromBody] SetSlotQuantityRequestDto request, CancellationToken ct)
    {
        try
        {
            var dto = new SetSlotQuantityRequestDto(slotId, productId, request.Quantity);
            var slot = await zoneAisleService.SetSlotQuantityAsync(dto, ct);
            return slot is null ? NotFound() : Ok(slot);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Tìm Slots chứa sản phẩm cụ thể.</summary>
    [HttpGet("products/{productId:int}/slots")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SlotDto>>> FindSlotsByProduct(int productId, CancellationToken ct)
    {
        var slots = await zoneAisleService.FindSlotsByProductAsync(productId, ct);
        return Ok(slots);
    }

    // ─── Aisle Density ─────────────────────────────────────────────────────

    /// <summary>
    /// Mật độ hàng hoá của từng kệ (Aisle) dựa trên lần AisleScan gần nhất.
    /// Trả kèm DensityColor: green ≥ 70%, yellow 40–69%, red &lt; 40%.
    /// </summary>
    [HttpGet("aisles/density")]
    [ProducesResponseType(typeof(IReadOnlyList<AisleDensityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AisleDensityDto>>> GetAisleDensities([FromQuery] int? zoneId, CancellationToken ct)
    {
        var densities = await zoneAisleService.GetAisleDensitiesAsync(zoneId, ct);
        return Ok(densities);
    }
}
