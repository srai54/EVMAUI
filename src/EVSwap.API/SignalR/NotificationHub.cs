using Microsoft.AspNetCore.SignalR;

namespace EVSwap.API.SignalR;

public class NotificationHub : Hub
{
    public async Task SendNotification(string userId, object data)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", data);
    }

    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
    }
}
