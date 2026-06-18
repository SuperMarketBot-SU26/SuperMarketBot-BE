namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// Flow 4 — Realtime cho Staff App.
/// Bạn A: fill implementation trong <c>StaffRealtimeService</c> (Infrastructure/Services).
/// </summary>
public interface IStaffRealtimeService
{
    /// <summary>Publish 1 alert tới tất cả Staff client đang subscribe <c>/hubs/staff</c>.</summary>
    Task PublishAlertAsync(Models.Realtime.StaffRealtimeAlertDto alert, CancellationToken ct = default);

    /// <summary>Broadcast số lượng alert chưa đọc cho dashboard Staff (khi query REST).</summary>
    Task<int> GetUnreadCountAsync(CancellationToken ct = default);
}