using Microsoft.AspNetCore.SignalR;

namespace EVSwap.API.SignalR;

public class StationHub : Hub
{
    public async Task JoinStationGroup(string stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"station_{stationId}");
    }

    public async Task LeaveStationGroup(string stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"station_{stationId}");
    }

    public async Task SendInventoryUpdate(string stationId, object data)
    {
        await Clients.Group($"station_{stationId}").SendAsync("InventoryUpdate", data);
    }
}
