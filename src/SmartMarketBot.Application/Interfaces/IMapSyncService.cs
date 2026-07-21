using SmartMarketBot.Application.Models.Maps;

namespace SmartMarketBot.Application.Interfaces;

public interface IMapSyncService
{
    Task<MapSyncResponseDto> SyncMapAsync(MapSyncRequestDto request, CancellationToken cancellationToken = default);
    Task<List<MapSummaryDto>> GetAllMapsAsync(int? floorId = null, CancellationToken cancellationToken = default);
    Task<MapFloorplanDto?> GetMapByIdAsync(int mapId, CancellationToken cancellationToken = default);
    Task<MapFloorplanDto?> GetLatestMapAsync(int floorId, CancellationToken cancellationToken = default);
    Task<MapSyncStatsDto> GetMapStatsAsync(int floorId, CancellationToken cancellationToken = default);
    Task<UploadFloorplanImageResponseDto> UploadFloorplanImageAsync(int mapId, Stream stream, string fileName, CancellationToken cancellationToken = default);
}
