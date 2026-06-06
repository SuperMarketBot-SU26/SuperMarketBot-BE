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
    INavigationService navigationService,
    ILocalizationService localizer) : IRobotService
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

    public async Task<RobotPoseDto> GetPoseAsync(string robotCode, CancellationToken cancellationToken = default)
    {
        var robot = await dbContext.Robots
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RobotCode == robotCode, cancellationToken)
            ?? throw new InvalidOperationException(localizer.Get("RobotNotFound", robotCode));

        var latestLog = await dbContext.RobotLogs
            .AsNoTracking()
            .Where(l => l.RobotID == robot.RobotID && l.XCoord.HasValue && l.YCoord.HasValue)
            .OrderByDescending(l => l.timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        double x = latestLog?.XCoord ?? 0;
        double y = latestLog?.YCoord ?? 0;
        double headingRad = latestLog?.HeadingRad ?? 0;
        double headingDeg = headingRad * 180.0 / Math.PI;

        return new RobotPoseDto(robotCode, x, y, headingRad, headingDeg, latestLog?.timestamp);
    }

    public async Task NavigateRobotAsync(NavigateRobotRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.WaypointNodeIds is { Count: > 0 })
        {
            var payloadIds = JsonSerializer.Serialize(new { waypoints = request.WaypointNodeIds });
            await commandPublisher.PublishCommandAsync(
                request.RobotCode,
                "navigate",
                payloadIds,
                cancellationToken);
            return;
        }

        var robot = await dbContext.Robots
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RobotCode == request.RobotCode, cancellationToken)
                ?? throw new InvalidOperationException(localizer.Get("RobotNotFound", request.RobotCode));

            if (!int.TryParse(request.DestinationNodeId, out int destId))
            {
                throw new ArgumentException(localizer.Get("DestNodeInvalid"));
            }

            var startNodeId = robot.CurrentNodeID ?? 1;

            var routeResult = await navigationService.PlanRouteAsync(
                new RoutePlanRequestDto(startNodeId, destId),
                cancellationToken);

            var waypoints = routeResult.Nodes
                .Select(n => new { x = n.X, y = n.Y, nodeId = n.NodeId })
                .ToList();

            var payloadWithCoords = JsonSerializer.Serialize(new { waypoints });
            await commandPublisher.PublishCommandAsync(
                request.RobotCode,
                "navigate",
                payloadWithCoords,
                cancellationToken);
    }
}
