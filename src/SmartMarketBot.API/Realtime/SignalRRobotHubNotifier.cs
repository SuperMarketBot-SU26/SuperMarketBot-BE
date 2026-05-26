using Microsoft.AspNetCore.SignalR;
using SmartMarketBot.API.Hubs;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Robots;

namespace SmartMarketBot.API.Realtime;

public sealed class SignalRRobotHubNotifier(IHubContext<RobotHub> hubContext) : IRobotHubNotifier
{
    public async Task NotifyTelemetryAsync(RobotTelemetryDto telemetry, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(RobotHub.GroupName(telemetry.RobotCode))
            .SendAsync("telemetry", telemetry, cancellationToken);
        await hubContext.Clients.All
            .SendAsync("telemetry", telemetry, cancellationToken);
    }

    public async Task NotifyStatusAsync(RobotStatusDto status, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(RobotHub.GroupName(status.RobotCode))
            .SendAsync("status", status, cancellationToken);
        await hubContext.Clients.All
            .SendAsync("status", status, cancellationToken);
    }
}
