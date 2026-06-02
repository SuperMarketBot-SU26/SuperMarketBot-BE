using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Application.Services;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NavigationController(
    INavigationService navigationService,
    NavigationCommandService navigationCommandService) : ControllerBase
{
    /// <summary>Tính route Dijkstra (không gửi xuống robot).</summary>
    [HttpPost("route")]
    public async Task<ActionResult<RoutePlanResultDto>> PlanRoute([FromBody] RoutePlanRequestDto request, CancellationToken cancellationToken)
    {
        var route = await navigationService.PlanRouteAsync(request, cancellationToken);
        return Ok(route);
    }

    /// <summary>
    /// Tính route Dijkstra và gửi waypoints (x, y, nodeId) xuống robot qua MQTT — dùng khi test AMR.
    /// </summary>
    [HttpPost("navigate")]
    public async Task<IActionResult> NavigateRobot(
        [FromBody] NavigateMapRequestDto request,
        CancellationToken cancellationToken)
    {
        await navigationCommandService.SendNavigationAsync(
            request.RobotCode,
            request.StartNodeId,
            request.EndNodeId,
            cancellationToken);
        return Accepted(new
        {
            message = $"Navigate command sent to {request.RobotCode}.",
            request.StartNodeId,
            request.EndNodeId
        });
    }

    /// <summary>
    /// Phase 3 — Reroute: đánh dấu nodes bị chặn, tính lại đường đi và gửi lệnh navigate xuống robot.
    /// </summary>
    [HttpPost("reroute")]
    public async Task<IActionResult> Reroute([FromBody] RerouteRequestDto request, CancellationToken cancellationToken)
    {
        await navigationCommandService.RerouteAsync(request, cancellationToken);
        return Accepted(new { message = $"Reroute command sent to robot {request.RobotCode}." });
    }

    /// <summary>Phase 3 — Unblock nodes sau khi robot đi qua.</summary>
    [HttpPost("unblock-nodes")]
    public async Task<IActionResult> UnblockNodes([FromBody] UnblockNodesRequestDto request, CancellationToken cancellationToken)
    {
        await navigationCommandService.UnblockNodesAsync(request.NodeIds, cancellationToken);
        return Ok(new { message = $"Unblocked {request.NodeIds.Count} node(s)." });
    }
}
