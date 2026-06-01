using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Application.Models.Robots;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class RobotService(
    AppDbContext dbContext,
    IRobotCommandPublisher commandPublisher,
    INavigationService navigationService) : IRobotService
{
    public async Task<IReadOnlyList<RobotDto>> GetRobotsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Robots
            .AsNoTracking()
            .OrderBy(x => x.RobotID)
            .Select(x => new RobotDto(
                x.RobotID,
                x.RobotName,
                x.RobotCode,
                x.BatteryPct,
                x.Mode,
                x.IsOnline,
                x.LastSeenAt))
            .ToListAsync(cancellationToken);
    }

    public Task PublishCommandAsync(PublishRobotCommandRequestDto request, CancellationToken cancellationToken = default)
    {
        return commandPublisher.PublishCommandAsync(request.RobotCode, request.Command, request.Payload, cancellationToken);
    }

    public async Task NavigateRobotAsync(NavigateRobotRequestDto request, CancellationToken cancellationToken = default)
    {
        List<string> waypointIds;

        if (request.WaypointNodeIds is { Count: > 0 })
        {
            waypointIds = request.WaypointNodeIds;
        }
        else
        {
            /* Tìm robot hiện tại để lấy CurrentNodeId làm điểm xuất phát */
            var robot = await dbContext.Robots
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RobotCode == request.RobotCode, cancellationToken)
                ?? throw new InvalidOperationException($"Robot '{request.RobotCode}' not found.");

            if (!int.TryParse(request.DestinationNodeId, out int destId))
            {
                throw new ArgumentException("DestinationNodeId phải là số nguyên hợp lệ.");
            }

            var startNodeId = robot.CurrentNodeID ?? 1;

            var routeResult = await navigationService.PlanRouteAsync(
                new RoutePlanRequestDto(startNodeId, destId),
                cancellationToken);

            waypointIds = routeResult.Nodes
                .Select(n => n.NodeId.ToString())
                .ToList();
        }

        var payload = JsonSerializer.Serialize(new { waypoints = waypointIds });
        await commandPublisher.PublishCommandAsync(
            request.RobotCode,
            "navigate",
            payload,
            cancellationToken);
    }
}
