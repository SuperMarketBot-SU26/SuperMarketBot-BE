using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.RobotRoutes;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// CRUD lộ trình cố định (RobotRoute) của robot.
/// GET /routes/{id} trả waypoints [{x,y,nodeId}] để Android Robot vẽ polyline trên canvas.
/// </summary>
[ApiController]
[Route("api/v1/routes")]
public sealed class RobotRoutesController(
    IRobotRouteService routeService,
    ILocalizationService localizer) : ControllerBase
{
    /// <summary>Lấy toàn bộ routes của 1 map. Hỗ trợ lọc theo zoneId và routeType.</summary>
    /// GET /api/v1/routes?mapId=1&amp;zoneId=2&amp;routeType=patrol
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RobotRouteListDto>>> GetRoutes(
        [FromQuery] int mapId,
        [FromQuery] int? zoneId = null,
        [FromQuery] string? routeType = null,
        CancellationToken cancellationToken = default)
    {
        if (mapId <= 0)
            return BadRequest(new { message = localizer.Get("MapIdRequired") });

        var routes = await routeService.GetRoutesByMapAsync(mapId, zoneId, routeType, cancellationToken);
        return Ok(routes);
    }

    /// <summary>Lấy chi tiết route kèm danh sách waypoints tọa độ (đã sắp xếp theo SequenceOrder).
    /// Response này gửi thẳng xuống Android Robot để vẽ đường đi trên canvas bản đồ.</summary>
    [HttpGet("{routeId:int}")]
    public async Task<ActionResult<RobotRouteDetailDto>> GetRoute(
        int routeId,
        CancellationToken cancellationToken = default)
    {
        var route = await routeService.GetRouteByIdAsync(routeId, cancellationToken);
        if (route is null)
            return NotFound(new { message = localizer.Get("RouteNotFound", routeId) });

        return Ok(route);
    }

    /// <summary>Tạo route mới. Tự động tạo RouteNodeMapping với SequenceOrder tăng dần từ 0.</summary>
    [HttpPost]
    public async Task<ActionResult<RobotRouteResultDto>> CreateRoute(
        [FromBody] RobotRouteCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.MapId <= 0)
            return BadRequest(new { message = localizer.Get("MapIdRequired") });
        if (string.IsNullOrWhiteSpace(dto.RouteName))
            return BadRequest(new { message = localizer.Get("RouteNameRequired") });

        try
        {
            var result = await routeService.CreateRouteAsync(dto, cancellationToken);
            return CreatedAtAction(
                nameof(GetRoute),
                new { routeId = result.RobotRouteId },
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Cập nhật route: ghi đè RouteName/RouteType/Description/ZoneId và danh sách waypoints.</summary>
    [HttpPut("{routeId:int}")]
    public async Task<ActionResult<RobotRouteResultDto>> UpdateRoute(
        int routeId,
        [FromBody] RobotRouteUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await routeService.UpdateRouteAsync(routeId, dto, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Xóa route (tự động cascade xóa RouteNodeMapping).</summary>
    [HttpDelete("{routeId:int}")]
    public async Task<ActionResult> DeleteRoute(
        int routeId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await routeService.DeleteRouteAsync(routeId, cancellationToken);
        if (!deleted)
            return NotFound(new { message = localizer.Get("RouteNotFound", routeId) });

        return NoContent();
    }
}
