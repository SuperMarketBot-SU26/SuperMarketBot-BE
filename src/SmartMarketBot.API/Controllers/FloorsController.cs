using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// Cung cấp danh sách tầng trong hệ thống để render dropdown chọn tầng
/// (Web Manager floor switcher, Mobile App, Robot dashboard).
/// </summary>
[ApiController]
[Route("api/v1")]
[AllowAnonymous]
public sealed class FloorsController(IFloorService floorService) : ControllerBase
{
    /// <summary>
    /// Lấy toàn bộ tầng trong siêu thị, sắp xếp theo FloorNumber tăng dần.
    /// GET /api/v1/floors
    /// </summary>
    [HttpGet("floors")]
    [ProducesResponseType(typeof(IReadOnlyList<FloorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FloorDto>>> GetFloors(CancellationToken cancellationToken = default)
    {
        var floors = await floorService.GetFloorsAsync(cancellationToken);
        return Ok(floors);
    }

    /// <summary>
    /// Lấy chi tiết 1 tầng theo FloorId.
    /// GET /api/v1/floors/{floorId}
    /// </summary>
    [HttpGet("floors/{floorId:int}")]
    [ProducesResponseType(typeof(FloorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FloorDto>> GetFloorById(int floorId, CancellationToken cancellationToken = default)
    {
        var floor = await floorService.GetFloorByIdAsync(floorId, cancellationToken);
        return floor is null ? NotFound() : Ok(floor);
    }
}
