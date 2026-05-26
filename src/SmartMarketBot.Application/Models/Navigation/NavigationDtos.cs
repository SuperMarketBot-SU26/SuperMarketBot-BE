namespace SmartMarketBot.Application.Models.Navigation;

public sealed record RoutePlanRequestDto(int StartNodeId, int EndNodeId);

public sealed record RouteNodeDto(int NodeId, double X, double Y, double DistanceFromStart);

public sealed record RoutePlanResultDto(double TotalDistance, IReadOnlyList<RouteNodeDto> Nodes);
