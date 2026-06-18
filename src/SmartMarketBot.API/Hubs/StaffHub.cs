using Microsoft.AspNetCore.SignalR;

namespace SmartMarketBot.API.Hubs;

/// <summary>
/// SignalR hub cho Staff App — subscribe <c>/hubs/staff</c>.
/// Staff connect để nhận realtime alert (OOS, robot lỗi, nhiệm vụ restock, member dị ứng).
/// </summary>
public sealed class StaffHub : Hub
{
    public async Task JoinStaffGroup() => await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);

    public async Task LeaveStaffGroup() => await Groups.RemoveFromGroupAsync(Context.ConnectionId, StaffGroup);

    public const string StaffGroup = "staff";
}