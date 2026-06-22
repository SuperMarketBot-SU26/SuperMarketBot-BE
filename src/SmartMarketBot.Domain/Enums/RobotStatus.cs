namespace SmartMarketBot.Domain.Enums;

/// <summary>
/// Trạng thái vận hành của robot. Lưu trong DB dưới dạng string (NVARCHAR(50)).
/// Đồng bộ với docs/phases và MQTT payload từ ESP32-S3.
/// </summary>
public enum RobotStatus
{
    /// <summary>Robot đã tắt nguồn hoàn toàn.</summary>
    Power_Off,

    /// <summary>Robot đang bật nhưng rảnh — chờ nhiệm vụ.</summary>
    Idle,

    /// <summary>Robot đang di chuyển theo route (navigate Dijkstra).</summary>
    Moving,

    /// <summary>Robot đang tương tác với khách (nhận diện khuôn mặt, scan kệ, giao hàng).</summary>
    Interacting,

    /// <summary>Robot đang ngoại tuyến / đang sạc pin tại trạm.</summary>
    Offline_Charging
}

public static class RobotStatusExtensions
{
    public static string ToDbString(this RobotStatus status) => status.ToString();

    public static bool TryParseDbString(string? value, out RobotStatus status)
        => Enum.TryParse(value, ignoreCase: true, out status);

    public static IReadOnlyList<string> AllDbStrings { get; } =
        Enum.GetValues<RobotStatus>().Select(s => s.ToString()).ToList();
}
