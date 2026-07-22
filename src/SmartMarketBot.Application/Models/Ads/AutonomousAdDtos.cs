namespace SmartMarketBot.Application.Models.Ads;

public sealed record ZonePlaylistResponseDto(
    int RobotId,
    int ZoneId,
    string? ZoneName,
    List<RobotPlaylistItemDto> Playlist,
    DateTime GeneratedAt);

public sealed record AutonomousRouteDto(
    int AdRouteId,
    string RouteName,
    string? Description,
    bool IsAutonomous,
    int? SemanticObjectId,
    List<AutonomousRouteStopDto> Stops,
    DateTime GeneratedAt);

public sealed record AutonomousRouteStopDto(
    int SequenceOrder,
    int NodeId,
    string? NodeName,
    int DwellTimeSeconds,
    int? ZoneId,
    string? ZoneName,
    List<RobotPlaylistItemDto> Playlist);

public sealed record AssignAutonomousRouteRequestDto
{
    public required int AdRouteId { get; init; }
}

// === NEW: Broadcast DTOs ===
public sealed record AdPlaylistDto(
    int RobotId,
    string Mode,
    int? NodeId,
    int? ZoneId,
    string? ZoneName,
    List<RobotPlaylistItemDto> Resources,
    DateTime GeneratedAt);

public sealed record AdRouteBroadcastDto(
    int RobotId,
    int AdRouteId,
    string RouteName,
    bool IsAutonomous,
    List<AutonomousRouteStopDto> Stops,
    DateTime GeneratedAt);
