using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

public interface IAdRouteService
{
    Task<PaginatedResponse<AdRouteResponseDto>> GetListAsync(int pageNumber, int pageSize, bool? isActive, CancellationToken cancellationToken = default);
    Task<AdRouteResponseDto?> GetByIdAsync(int routeId, CancellationToken cancellationToken = default);
    Task<AdRouteResponseDto> CreateAsync(CreateAdRouteRequestDto request, CancellationToken cancellationToken = default);
    Task<AdRouteResponseDto> UpdateAsync(int routeId, UpdateAdRouteRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int routeId, CancellationToken cancellationToken = default);
    Task<AdRouteResponseDto> AssignToRobotAsync(int routeId, int robotId, CancellationToken cancellationToken = default);
    Task<AdRouteResponseDto?> GetActiveRouteForRobotAsync(int robotId, CancellationToken cancellationToken = default);
}
