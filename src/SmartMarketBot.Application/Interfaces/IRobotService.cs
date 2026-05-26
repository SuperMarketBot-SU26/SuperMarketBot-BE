using SmartMarketBot.Application.Models.Robots;

namespace SmartMarketBot.Application.Interfaces;

public interface IRobotService
{
    Task<IReadOnlyList<RobotDto>> GetRobotsAsync(CancellationToken cancellationToken = default);
    Task PublishCommandAsync(PublishRobotCommandRequestDto request, CancellationToken cancellationToken = default);
}
