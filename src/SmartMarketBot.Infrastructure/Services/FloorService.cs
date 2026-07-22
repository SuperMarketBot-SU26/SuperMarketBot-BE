using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Maps;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class FloorService(AppDbContext db) : IFloorService
{
    public async Task<IReadOnlyList<FloorDto>> GetFloorsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Floors
            .AsNoTracking()
            .OrderBy(f => f.FloorNumber)
            .Select(f => new FloorDto(
                f.FloorId,
                f.FloorNumber,
                f.Zones.Count,
                f.Maps.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<FloorDto?> GetFloorByIdAsync(int floorId, CancellationToken cancellationToken = default)
    {
        return await db.Floors
            .AsNoTracking()
            .Where(f => f.FloorId == floorId)
            .Select(f => new FloorDto(
                f.FloorId,
                f.FloorNumber,
                f.Zones.Count,
                f.Maps.Count))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
