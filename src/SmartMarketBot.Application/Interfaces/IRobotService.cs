using SmartMarketBot.Application.Models.Robots;

namespace SmartMarketBot.Application.Interfaces;

public interface IRobotService
{
    Task<IReadOnlyList<RobotDto>> GetRobotsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy RobotDto theo RobotCode (dùng cho IoT endpoint tra cứu nhanh).
    /// </summary>
    Task<RobotDto?> GetByCodeAsync(string robotCode, CancellationToken cancellationToken = default);

    Task PublishCommandAsync(PublishRobotCommandRequestDto request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Tính đường đi (Dijkstra nếu cần) rồi publish lệnh navigate xuống robot qua MQTT.
    /// </summary>
    Task NavigateRobotAsync(NavigateRobotRequestDto request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Trả về pose mới nhất của robot từ log Dead Reckoning.
    /// </summary>
    Task<RobotPoseDto> GetPoseAsync(string robotCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật trạng thái robot (Power_Off / Idle / Moving / Interacting / Offline_Charging).
    /// Validate enum, cập nhật DB, ghi Robot_Logs, broadcast SignalR telemetry.
    /// </summary>
    Task<RobotDto> UpdateStatusAsync(string robotCode, UpdateRobotStatusRequestDto request, CancellationToken cancellationToken = default);
}
