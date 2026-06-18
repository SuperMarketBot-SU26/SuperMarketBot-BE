using SmartMarketBot.Application.Models.Realtime;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// Abstraction cho SignalR hub client (Staff side).
/// Bạn A: wrap <c>Microsoft.AspNetCore.SignalR.IHubContext&lt;StaffHub&gt;</c> bằng interface này để dễ test/mock.
/// </summary>
public interface IStaffRealtimeNotifier
{
    Task BroadcastAlertAsync(StaffRealtimeAlertDto alert, CancellationToken ct = default);
}