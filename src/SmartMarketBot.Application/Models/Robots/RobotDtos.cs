namespace SmartMarketBot.Application.Models.Robots;

public sealed record RobotDto(
    int RobotId,
    string RobotName,
    string RobotCode,
    int BatteryPct,
    string Mode,
    string Status,
    DateTime? LastSeenAt);

public sealed record PublishRobotCommandRequestDto(string RobotCode, string Command, string? Payload);

/// <summary>
/// MQTT telemetry từ ESP32-S3 — Phase 1 bổ sung LiDAR + RPM + NavState.
/// </summary>
public sealed record RobotTelemetryDto(
    string RobotCode,
    int? Battery,
    string? Location,
    string? Status,
    int? CurrentNodeId,
    string? Mode,
    bool? IsOnline,
    double? XCoord,
    double? YCoord,
    DateTime TimestampUtc,
    // Phase 1 sensor extensions
    int? LidarFront = null,
    int? LidarRear = null,
    double? RpmFL = null,
    double? RpmFR = null,
    double? RpmRL = null,
    double? RpmRR = null,
    string? NavState = null,
    bool? Estop = null);

public sealed record RobotStatusDto(
    string RobotCode,
    int? Battery,
    string? Location,
    string? Status,
    string? Mode,
    bool? IsOnline,
    DateTime TimestampUtc);

/// <summary>
/// Pose hiện tại của robot từ Dead Reckoning (Phase 2).
/// </summary>
public sealed record RobotPoseDto(
    string RobotCode,
    double X,
    double Y,
    double HeadingRad,
    double HeadingDeg,
    DateTime? TimestampUtc);

/// <summary>
/// Lệnh điều hướng — gửi xuống robot qua MQTT topic .../command.
/// </summary>
public sealed record NavigateRobotRequestDto(
    string RobotCode,
    string DestinationNodeId,
    /// <summary>Danh sách node IDs theo thứ tự — null thì backend tự tính Dijkstra.</summary>
    List<string>? WaypointNodeIds = null);
