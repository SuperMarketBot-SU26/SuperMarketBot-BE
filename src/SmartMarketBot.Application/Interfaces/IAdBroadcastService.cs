using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

public interface IAdBroadcastService
{
    /// <summary>
    /// Get current ad playlist based on robot position.
    /// Automatically detects mode (Autonomous vs Zone/Shelf).
    /// </summary>
    Task<AdPlaylistDto> GetPlaylistForRobotAsync(
        int robotId,
        int x, int y,
        CancellationToken ct = default);

    /// <summary>
    /// Get full autonomous route with pre-compiled playlists per stop.
    /// Called once at route start.
    /// </summary>
    Task<AdRouteBroadcastDto?> GetAutonomousRoutePlaylistAsync(
        int robotId,
        CancellationToken ct = default);

    /// <summary>
    /// Get ads for Zone/Shelf mode based on spatial position (AABB detection).
    /// </summary>
    Task<AdPlaylistDto> GetZoneShelfPlaylistAsync(
        int robotId,
        int x, int y,
        CancellationToken ct = default);

    /// <summary>
    /// Get ads for a specific node in Autonomous mode.
    /// </summary>
    Task<AdPlaylistDto> GetPlaylistForNodeAsync(
        int robotId,
        int nodeId,
        CancellationToken ct = default);
}
