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

    public async Task<RobotPoseDto> GetPoseAsync(string robotCode, CancellationToken cancellationToken = default)
    {
        var robot = await dbContext.Robots
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RobotCode == robotCode, cancellationToken)
            ?? throw new InvalidOperationException($"Robot '{robotCode}' not found.");

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
