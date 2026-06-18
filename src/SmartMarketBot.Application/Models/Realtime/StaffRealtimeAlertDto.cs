namespace SmartMarketBot.Application.Models.Realtime;

/// <summary>Payload push xuống Staff App khi có sự cố OOS, Robot lỗi, hoặc nhiệm vụ mới.</summary>
public sealed record StaffRealtimeAlertDto(
    int AlertId,
    string AlertType,           // "OOS" | "RestockTask" | "RobotError" | "MemberAllergy"
    string Severity,            // "info" | "warning" | "critical"
    string Title,
    string Message,
    int? ZoneId,
    int? AisleId,
    int? SlotId,
    int? RobotId,
    int? MemberId,
    DateTime Timestamp);