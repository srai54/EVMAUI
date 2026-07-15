using Microsoft.AspNetCore.SignalR;

namespace EVSwap.API.SignalR;

public class BatteryHub : Hub
{
    public async Task SendBatteryUpdate(int batteryId, object data)
    {
        await Clients.Group($"battery_{batteryId}").SendAsync("BatteryUpdate", data);
    }

    public async Task SendSwapRequestUpdate(object data)
    {
        await Clients.All.SendAsync("SwapRequestUpdate", data);
    }

    public async Task JoinBatteryGroup(int batteryId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"battery_{batteryId}");
    }

    public async Task LeaveBatteryGroup(int batteryId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"battery_{batteryId}");
    }
}
