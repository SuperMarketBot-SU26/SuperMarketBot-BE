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
    List<AutonomousRouteStopDto> Stops,
    DateTime GeneratedAt);

public sealed record AutonomousRouteStopDto(
    int SequenceOrder,
    int NodeId,
    string? NodeName,
    int DwellTimeSeconds,
    List<RobotPlaylistItemDto> Playlist);

public sealed record AssignAutonomousRouteRequestDto
{
    public required int AdRouteId { get; init; }
}
