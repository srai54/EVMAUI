using Microsoft.AspNetCore.SignalR;

namespace EVSwap.API.SignalR;

public class DashboardHub : Hub
{
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
    }

    public async Task SendDashboardUpdate(object data)
    {
        await Clients.Group("AdminGroup").SendAsync("DashboardUpdate", data);
    }
}
