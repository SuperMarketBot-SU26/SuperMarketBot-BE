namespace SmartMarketBot.Application.Models.Navigation;

public sealed record RoutePlanRequestDto(int StartNodeId, int EndNodeId);

/// <summary>Gửi lệnh navigate xuống robot (Dijkstra + MQTT waypoints có tọa độ).</summary>
public sealed record NavigateMapRequestDto(string RobotCode, int StartNodeId, int EndNodeId);

public sealed record RouteNodeDto(int NodeId, double X, double Y, double DistanceFromStart);

public sealed record RoutePlanResultDto(double TotalDistance, IReadOnlyList<RouteNodeDto> Nodes);

/// <summary>
/// Phase 3 — Yêu cầu reroute khi có chướng ngại vật mới trên đường.
/// </summary>
public sealed record RerouteRequestDto(
    string RobotCode,
    int CurrentNodeId,
    int DestinationNodeId,
    List<int>? BlockedNodeIds = null);

/// <summary>Phase 3 — Unblock danh sách nodes sau khi robot đi qua.</summary>
public sealed record UnblockNodesRequestDto(List<int> NodeIds);

/// <summary>
/// API Mobile — Tìm đường đi từ tọa độ (startX, startY) đến một SemanticObject (endObjectId).
/// Mobile App dùng endpoint này để vẽ Polyline chỉ đường.
/// </summary>
public sealed record MobileRouteRequestDto(
    double StartX,
    double StartY,
    int? EndObjectId,
    int? EndNodeId);

public sealed record MobileRoutePointDto(
    double X,
    double Y,
    int? NodeId,
    string? Description);

public sealed record MobileRouteResponseDto(
    double TotalDistance,
    int EstimatedTimeSeconds,
    IReadOnlyList<MobileRoutePointDto> Path);
