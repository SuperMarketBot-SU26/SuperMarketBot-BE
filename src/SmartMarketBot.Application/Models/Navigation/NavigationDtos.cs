namespace SmartMarketBot.Application.Models.Navigation;

public sealed record RoutePlanRequestDto(int StartNodeId, int EndNodeId);

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
