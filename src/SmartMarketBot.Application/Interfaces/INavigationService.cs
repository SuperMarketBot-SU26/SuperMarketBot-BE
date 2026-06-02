using SmartMarketBot.Application.Models.Navigation;

namespace SmartMarketBot.Application.Interfaces;

public interface INavigationService
{
    Task<RoutePlanResultDto> PlanRouteAsync(RoutePlanRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flow 1 — Tối ưu hoá lộ trình mua sắm đa điểm:
    /// TSP (Nearest Neighbour) + Dijkstra per-segment + loại ForbiddenZones.
    /// </summary>
    Task<OptimizeShoppingRouteResponseDto> OptimizeShoppingRouteAsync(OptimizeShoppingRouteRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Flow 1 — Block/Unblock NavigationNode theo thời gian thực.</summary>
    Task SetNodeBlockedAsync(int nodeId, bool isBlocked, string? reason, CancellationToken cancellationToken = default);
}
