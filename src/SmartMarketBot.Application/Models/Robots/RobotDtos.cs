namespace SmartMarketBot.Application.Models.Robots;

public sealed record RobotDto(
    int RobotId,
    string RobotName,
    string RobotCode,
    int BatteryPct,
    string Mode,
    bool IsOnline,
    DateTime? LastSeenAt);

public sealed record PublishRobotCommandRequestDto(string RobotCode, string Command, string? Payload);

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
    DateTime TimestampUtc);

public sealed record RobotStatusDto(
    string RobotCode,
    int? Battery,
    string? Location,
    string? Status,
    string? Mode,
    bool? IsOnline,
    DateTime TimestampUtc);
