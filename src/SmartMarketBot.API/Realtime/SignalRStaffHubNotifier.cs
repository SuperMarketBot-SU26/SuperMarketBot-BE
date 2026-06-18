using Microsoft.AspNetCore.SignalR;
using SmartMarketBot.API.Hubs;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Realtime;

namespace SmartMarketBot.API.Realtime;

/// <summary>Skeleton — Bạn A fill implementation chi tiết nếu cần filter theo Zone/StaffRole.</summary>
public sealed class SignalRStaffHubNotifier(IHubContext<StaffHub> hubContext) : IStaffRealtimeNotifier
{
    public async Task BroadcastAlertAsync(StaffRealtimeAlertDto alert, CancellationToken ct = default)
    {
        await hubContext.Clients.Group(StaffHub.StaffGroup).SendAsync("staffAlert", alert, ct);
    }
}