using Microsoft.AspNetCore.Authorization;
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
    /// <summary>
    /// API Mobile — Tìm đường đi từ tọa độ (startX, startY) đến SemanticObject hoặc NavigationNode đích.
    /// Mobile App dùng endpoint này để vẽ Polyline chỉ đường trên bản đồ.
    /// </summary>
    [HttpGet("route")]
    [AllowAnonymous]
    public async Task<ActionResult<MobileRouteResponseDto>> FindMobileRoute(
        [FromQuery] double startX,
        [FromQuery] double startY,
        [FromQuery] int? endObjectId,
        [FromQuery] int? endNodeId,
        CancellationToken cancellationToken)
    {
        var request = new MobileRouteRequestDto(startX, startY, endObjectId, endNodeId);
        var result = await navigationService.FindMobileRouteAsync(request, cancellationToken);
        return Ok(result);
    }

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

    /// <summary>
    /// Flow 1 — Tối ưu hoá lộ trình mua sắm đa điểm (TSP + Dijkstra + ForbiddenZones).
    /// Trả về danh sách Waypoints được sắp xếp thứ tự ngắn nhất để robot ghé qua tất cả kệ sản phẩm.
    /// </summary>
    [HttpPost("optimize-shopping-route")]
    [AllowAnonymous]
    public async Task<ActionResult<OptimizeShoppingRouteResponseDto>> OptimizeShoppingRoute(
        [FromBody] OptimizeShoppingRouteRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await navigationService.OptimizeShoppingRouteAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Flow 1 — Block/Unblock một NavigationNode cụ thể theo thời gian thực.
    /// VD: Robot phát hiện người cản, tràn dầu, thi công tạm thời.
    /// </summary>
    [HttpPost("nodes/{id:int}/block")]
    [AllowAnonymous]
    public async Task<IActionResult> BlockNode(
        int id,
        [FromBody] BlockNodeRequestDto request,
        CancellationToken cancellationToken)
    {
        await navigationService.SetNodeBlockedAsync(id, request.IsBlocked, request.Reason, cancellationToken);
        return Ok(new { nodeId = id, isBlocked = request.IsBlocked, reason = request.Reason });
    }
}
