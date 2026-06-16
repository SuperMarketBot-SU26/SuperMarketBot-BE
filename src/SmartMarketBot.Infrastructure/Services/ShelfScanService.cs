using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.AisleScans;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AisleScanService(AppDbContext dbContext) : IAisleScanService
{
    public async Task<IReadOnlyList<ShelfScanDto>> GetRecentScansAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 20;
        }

        return await dbContext.AisleScans
            .AsNoTracking()
            .OrderByDescending(x => x.ScannedAt)
            .Take(take)
            .Select(x => new ShelfScanDto(
                x.ScanId,
                x.AisleId,
                0, // ShelfId - not in AisleScan
                x.RobotId,
                x.ScannedAt,
                x.EmptyPercentage,
                x.NeedsRestock,
                x.ImageUrl,
                x.AiResponseRaw))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShelfScanDto> CreateScanAsync(CreateAisleScanRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = new AisleScan
        {
            AisleId = request.AisleId,
            RobotId = request.RobotId,
            EmptyPercentage = request.EmptyPercentage,
            ImageUrl = request.ImageUrl,
            AiResponseRaw = request.AiResponseRaw,
            ScannedAt = VnDateTime.Now
        };

        dbContext.AisleScans.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ShelfScanDto(
            entity.ScanId,
            entity.AisleId,
            0, // ShelfId - not in AisleScan
            entity.RobotId,
            entity.ScannedAt,
            entity.EmptyPercentage,
            entity.NeedsRestock,
            entity.ImageUrl,
            entity.AiResponseRaw);
    }
}
