using Microsoft.AspNetCore.SignalR;
using SmartMarketBot.API.Hubs;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Realtime;

namespace SmartMarketBot.API.Realtime;

/// <summary>Skeleton — Bạn B fill implementation: push tới <c>member:{id}</c> group.</summary>
public sealed class SignalRMemberHubNotifier(IHubContext<MemberHub> hubContext) : IMemberRealtimeNotifier
{
    public async Task PushToMemberAsync(int memberId, MemberRealtimeUpdateDto update, CancellationToken ct = default)
    {
        await hubContext.Clients.Group(MemberHub.Group(memberId)).SendAsync("memberUpdate", update, ct);
    }
}