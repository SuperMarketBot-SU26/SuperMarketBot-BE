using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.ShelfScans;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class ShelfScanService(AppDbContext dbContext) : IShelfScanService
{
    public async Task<IReadOnlyList<ShelfScanDto>> GetRecentScansAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 20;
        }

        return await dbContext.ShelfScans
            .AsNoTracking()
            .OrderByDescending(x => x.ScannedAt)
            .Take(take)
            .Select(x => new ShelfScanDto(
                x.ScanID,
                x.AisleID,
                x.ShelfLevelID,
                x.RobotID,
                x.ScannedAt,
                x.EmptyPercentage,
                x.NeedsRestock,
                x.ImageUrl,
                x.AiResponseRaw))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShelfScanDto> CreateScanAsync(CreateShelfScanRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = new ShelfScan
        {
            AisleID = request.AisleId,
            ShelfLevelID = request.ShelfLevelId,
            RobotID = request.RobotId,
            EmptyPercentage = request.EmptyPercentage,
            ImageUrl = request.ImageUrl,
            AiResponseRaw = request.AiResponseRaw,
            ScannedAt = DateTime.UtcNow
        };

        dbContext.ShelfScans.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ShelfScanDto(
            entity.ScanID,
            entity.AisleID,
            entity.ShelfLevelID,
            entity.RobotID,
            entity.ScannedAt,
            entity.EmptyPercentage,
            entity.NeedsRestock,
            entity.ImageUrl,
            entity.AiResponseRaw);
    }
}
