using SmartMarketBot.Application.Models.Maps;

namespace SmartMarketBot.Application.Interfaces;

public interface IFloorService
{
    Task<IReadOnlyList<FloorDto>> GetFloorsAsync(CancellationToken cancellationToken = default);
    Task<FloorDto?> GetFloorByIdAsync(int floorId, CancellationToken cancellationToken = default);
}
