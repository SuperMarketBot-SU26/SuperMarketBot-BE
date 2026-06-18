using Microsoft.AspNetCore.SignalR;

namespace SmartMarketBot.API.Hubs;

/// <summary>
/// SignalR hub cho Member App — subscribe <c>/hubs/member</c>.
/// Member connect → gọi <c>JoinMemberGroup(memberId)</c> để nhận update realtime.
/// </summary>
public sealed class MemberHub : Hub
{
    public async Task JoinMemberGroup(int memberId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(memberId));

    public async Task LeaveMemberGroup(int memberId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(memberId));

    public static string Group(int memberId) => $"member:{memberId}";
}