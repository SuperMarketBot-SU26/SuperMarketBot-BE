namespace SmartMarketBot.Application.Interfaces;

public interface IRobotCommandPublisher
{
    Task PublishCommandAsync(string robotCode, string command, string? payload, CancellationToken cancellationToken = default);
}
