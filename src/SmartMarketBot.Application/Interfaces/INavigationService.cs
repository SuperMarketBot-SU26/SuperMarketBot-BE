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

    /// <summary>
    /// API Mobile — Tìm đường đi từ tọa độ (startX, startY) đến SemanticObject hoặc NavigationNode đích.
    /// Tự động tìm NavigationNode gần nhất với (startX, startY) và gần nhất với đích.
    /// </summary>
    Task<MobileRouteResponseDto> FindMobileRouteAsync(MobileRouteRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase B Step 2 — Tính route theo NodeCode (firmware line-scan).
    /// Resolve NodeCode → NodeId trong DB; algorithm Dijkstra giống PlanRouteAsync
    /// nhưng input/output dùng NodeCode (firmware không biết NodeId).
    /// </summary>
    Task<RouteLinePlanResultDto> PlanLineRouteAsync(RouteLinePlanRequestDto request, CancellationToken cancellationToken = default);
}
