using Microsoft.AspNetCore.SignalR;

namespace SmartMarketBot.API.Hubs;

public sealed class RobotHub : Hub
{
    public async Task JoinRobotGroup(string robotCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(robotCode));
    }

    public async Task LeaveRobotGroup(string robotCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(robotCode));
    }

    public static string GroupName(string robotCode) => $"robot:{robotCode}";
}
