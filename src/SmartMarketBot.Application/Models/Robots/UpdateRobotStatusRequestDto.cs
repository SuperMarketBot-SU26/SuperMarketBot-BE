namespace SmartMarketBot.Application.Models.Robots;

/// <summary>
/// Request cập nhật trạng thái robot. Status bắt buộc thuộc enum
/// Power_Off | Idle | Moving | Interacting | Offline_Charging (case-insensitive).
/// </summary>
public sealed record UpdateRobotStatusRequestDto(
    /// <summary>Enum string: Power_Off | Idle | Moving | Interacting | Offline_Charging.</summary>
    string Status,
    /// <summary>Pin (0-100). Nếu null → giữ nguyên giá trị hiện tại.</summary>
    int? BatteryPct = null,
    /// <summary>Mode tùy ý (vd: "follow", "patrol", "return_to_charge").</summary>
    string? Mode = null,
    /// <summary>Tọa độ hiện tại (tùy chọn, lưu vào Robot_Logs).</summary>
    double? XCoord = null,
    double? YCoord = null);
