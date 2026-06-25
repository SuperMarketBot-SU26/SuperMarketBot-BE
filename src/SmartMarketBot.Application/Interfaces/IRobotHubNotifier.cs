using SmartMarketBot.Application.Models.Robots;

namespace SmartMarketBot.Application.Interfaces;

public interface IRobotHubNotifier
{
    Task NotifyTelemetryAsync(RobotTelemetryDto telemetry, CancellationToken cancellationToken = default);
    Task NotifyStatusAsync(RobotStatusDto status, CancellationToken cancellationToken = default);
    Task NotifyLogAsync(string robotCode, string message, CancellationToken cancellationToken = default);
}
