using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Robots;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class RobotService(
    AppDbContext dbContext,
    IRobotCommandPublisher commandPublisher) : IRobotService
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
}
