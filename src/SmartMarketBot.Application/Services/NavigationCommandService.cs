using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Application.Models.Robots;
using SmartMarketBot.Domain.Entities;

namespace SmartMarketBot.Application.Services;

/// <summary>
/// Phase 3: Orchestrate navigation commands — tính route, block node tạm thời, reroute.
/// </summary>
public sealed class NavigationCommandService(
    IAppDbContext dbContext,
    INavigationService navigationService,
    IRobotCommandPublisher commandPublisher)
{
    /// <summary>
    /// Gửi lệnh navigate đến robot với danh sách waypoint đầy đủ (x, y, nodeId).
    /// Backend tính Dijkstra → gửi JSON payload gồm coords + nodeId cho robot.
    /// </summary>
    public async Task SendNavigationAsync(string robotCode, int startNodeId, int endNodeId, CancellationToken ct = default)
    {
        var route = await navigationService.PlanRouteAsync(
            new RoutePlanRequestDto(startNodeId, endNodeId), ct);

        if (route.Nodes.Count == 0)
        {
            throw new InvalidOperationException(
                $"No route found from node {startNodeId} to node {endNodeId}.");
        }

        var waypoints = route.Nodes
            .Select(n => new { x = n.X, y = n.Y, nodeId = n.NodeId })
            .ToList();

        var payload = JsonSerializer.Serialize(new { waypoints });
        await commandPublisher.PublishCommandAsync(robotCode, "navigate", payload, ct);
    }

    /// <summary>
    /// Reroute: đánh dấu node bị chặn tạm thời rồi tính lại đường đi và gửi lệnh mới.
    /// </summary>
    public async Task RerouteAsync(RerouteRequestDto request, CancellationToken ct = default)
    {
        /* Đánh dấu blocked nodes */
        if (request.BlockedNodeIds is { Count: > 0 })
        {
            var blockedNodes = await dbContext.NavigationNodes
                .Where(n => request.BlockedNodeIds.Contains(n.NodeID))
                .ToListAsync(ct);

            foreach (var node in blockedNodes)
            {
                node.IsBlocked = true;
            }
            await dbContext.SaveChangesAsync(ct);
        }

        /* Tính lại route — blocked nodes sẽ bị bỏ qua trong Dijkstra */
        await SendNavigationAsync(request.RobotCode, request.CurrentNodeId, request.DestinationNodeId, ct);
    }

    /// <summary>
    /// Unblock nodes sau khi robot đã qua (cleanup sau mỗi task).
    /// </summary>
    public async Task UnblockNodesAsync(List<int> nodeIds, CancellationToken ct = default)
    {
        var nodes = await dbContext.NavigationNodes
            .Where(n => nodeIds.Contains(n.NodeID))
            .ToListAsync(ct);

        foreach (var node in nodes)
        {
            node.IsBlocked = false;
        }
        await dbContext.SaveChangesAsync(ct);
    }
}
