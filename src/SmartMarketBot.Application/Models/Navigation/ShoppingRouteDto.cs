namespace SmartMarketBot.Application.Models.Navigation;

// ─── Flow 1: Multi-stop Shopping Route ───────────────────────────────────────

/// <summary>Request tối ưu hoá lộ trình gom hàng đa điểm (TSP + Dijkstra + ForbiddenZones).</summary>
public sealed record OptimizeShoppingRouteRequestDto(
    int RobotId,
    int StartNodeId,
    IReadOnlyList<int> ProductIds);

/// <summary>Một điểm dừng trong lộ trình mua sắm tối ưu.</summary>
public sealed record ShoppingWaypointDto(
    int Order,
    int NodeId,
    string NodeName,
    double X,
    double Y,
    int ProductId,
    string ProductName,
    int? ShelfLevelId,
    string? SlotCode,
    string? ShelfLocation);

public sealed record OptimizeShoppingRouteResponseDto(
    double TotalDistanceMeters,
    int WaypointCount,
    IReadOnlyList<ShoppingWaypointDto> Waypoints,
    IReadOnlyList<int> ExcludedForbiddenZoneIds,
    string OptimizationNote);

/// <summary>Block/Unblock một node (POST /api/navigation/nodes/{id}/block).</summary>
public sealed record BlockNodeRequestDto(bool IsBlocked, string? Reason);
