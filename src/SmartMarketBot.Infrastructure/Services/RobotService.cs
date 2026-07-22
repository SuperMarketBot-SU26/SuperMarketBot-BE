using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Application.Models.Robots;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Enums;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class RobotService(
    AppDbContext dbContext,
    IRobotCommandPublisher commandPublisher,
    INavigationService navigationService,
    IRobotHubNotifier hubNotifier,
    ILocalizationService localizer,
    IMemoryCache memoryCache,
    ILogger<RobotService> logger) : IRobotService
{
    public async Task<IReadOnlyList<RobotDto>> GetRobotsAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "robots_list_all";
        if (memoryCache.TryGetValue(cacheKey, out IReadOnlyList<RobotDto>? cachedRobots) && cachedRobots is not null)
        {
            return cachedRobots;
        }

        var robots = await dbContext.Robots
            .AsNoTracking()
            .OrderBy(x => x.RobotId)
            .Select(x => new RobotDto(
                x.RobotId,
                x.RobotName,
                x.RobotCode,
                x.BatteryPct,
                x.Mode,
                x.Status,
                x.LastSeenAt,
                x.IPAddress))
            .ToListAsync(cancellationToken);

        memoryCache.Set(cacheKey, robots, TimeSpan.FromMilliseconds(1000));
        return robots;
    }

    public async Task<RobotDto?> GetByCodeAsync(string robotCode, CancellationToken cancellationToken = default)
    {
        return await dbContext.Robots
            .AsNoTracking()
            .Where(r => r.RobotCode == robotCode)
            .Select(r => new RobotDto(
                r.RobotId,
                r.RobotName,
                r.RobotCode,
                r.BatteryPct,
                r.Mode,
                r.Status,
                r.LastSeenAt,
                r.IPAddress))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task PublishCommandAsync(PublishRobotCommandRequestDto request, CancellationToken cancellationToken = default)
    {
        return commandPublisher.PublishCommandAsync(request.RobotCode, request.Command, request.Payload, cancellationToken);
    }

    public async Task<RobotPoseDto> GetPoseAsync(string robotCode, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"robot_pose_{robotCode}";
        if (memoryCache.TryGetValue(cacheKey, out RobotPoseDto? cachedPose) && cachedPose is not null)
        {
            return cachedPose;
        }

        var robot = await dbContext.Robots
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RobotCode == robotCode, cancellationToken)
            ?? throw new InvalidOperationException(localizer.Get("RobotNotFound", robotCode));

        var latestLog = await dbContext.RobotLogs
            .AsNoTracking()
            .Where(l => l.RobotId == robot.RobotId && l.XCoord.HasValue && l.YCoord.HasValue)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        double x = latestLog?.XCoord ?? 0;
        double y = latestLog?.YCoord ?? 0;
        double headingRad = latestLog?.HeadingRad ?? 0;
        double headingDeg = headingRad * 180.0 / Math.PI;

        var pose = new RobotPoseDto(robotCode, x, y, headingRad, headingDeg, latestLog?.Timestamp);
        memoryCache.Set(cacheKey, pose, TimeSpan.FromMilliseconds(500));
        return pose;
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

            // [P0-3/4 FIX] Lấy current node (trên bản đồ) từ log gần nhất — KHÔNG lấy RobotId.
            // Trước đây `Select(l => (int?)l.RobotId)` trả về Robot ID thay vì Node ID
            // → PlanRouteAsync fail hoặc trả route sai → navigate lệch sang node khác.
            // Nếu chưa có log nào có CurrentNodeId → fallback về node 1 (start node mặc định).
            var currentNodeId = await dbContext.RobotLogs
                .AsNoTracking()
                .Where(l => l.RobotId == robot.RobotId && l.CurrentNodeId != null)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => (int?)l.CurrentNodeId)
                .FirstOrDefaultAsync(cancellationToken) ?? 1;

            var routeResult = await navigationService.PlanRouteAsync(
                new RoutePlanRequestDto(currentNodeId, destId),
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

    public async Task<RobotDto> UpdateStatusAsync(
        string robotCode,
        UpdateRobotStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(robotCode))
            throw new ArgumentException("robotCode is required", nameof(robotCode));

        // 1. Validate status theo enum RobotStatus
        if (!RobotStatusExtensions.TryParseDbString(request.Status, out var newStatus))
        {
            throw new ArgumentException(
                $"Status không hợp lệ: '{request.Status}'. Hợp lệ: {string.Join(", ", RobotStatusExtensions.AllDbStrings)}");
        }

        if (request.BatteryPct is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(request), "BatteryPct phải trong [0, 100].");

        // 2. Tìm robot
        var robot = await dbContext.Robots
            .FirstOrDefaultAsync(r => r.RobotCode == robotCode, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("RobotNotFound", robotCode));

        // 3. Cập nhật field
        var dbStatusString = newStatus.ToDbString();
        robot.Status = dbStatusString;
        if (request.BatteryPct.HasValue) robot.BatteryPct = request.BatteryPct.Value;
        if (!string.IsNullOrWhiteSpace(request.Mode)) robot.Mode = request.Mode;
        robot.LastSeenAt = DateTime.UtcNow;

        // 4. Ghi Robot_Logs (audit + telemetry history)
        var log = new Domain.Entities.RobotLog
        {
            RobotId = robot.RobotId,
            Battery = robot.BatteryPct,
            Status = dbStatusString,
            Location = request.Mode ?? robot.Mode,
            Timestamp = VnDateTime.Now,
            XCoord = request.XCoord.HasValue ? (float)request.XCoord.Value : null,
            YCoord = request.YCoord.HasValue ? (float)request.YCoord.Value : null
        };
        dbContext.RobotLogs.Add(log);

        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Broadcast SignalR telemetry tới group robot:{code} + All
        try
        {
            var statusDto = new RobotStatusDto(
                RobotCode: robot.RobotCode,
                Battery: robot.BatteryPct,
                Location: request.Mode ?? robot.Mode,
                Status: dbStatusString,
                Mode: robot.Mode,
                IsOnline: newStatus != RobotStatus.Power_Off && newStatus != RobotStatus.Offline_Charging,
                TimestampUtc: DateTime.UtcNow);
            await hubNotifier.NotifyStatusAsync(statusDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[RobotService] Failed to broadcast status update");
        }

        logger.LogInformation(
            "[RobotService] Status updated: {Code} → {Status} (battery={Battery}%, mode={Mode})",
            robot.RobotCode, dbStatusString, robot.BatteryPct, robot.Mode);

        return new RobotDto(
            robot.RobotId,
            robot.RobotName,
            robot.RobotCode,
            robot.BatteryPct,
            robot.Mode,
            robot.Status,
            robot.LastSeenAt,
            robot.IPAddress);
    }
}
