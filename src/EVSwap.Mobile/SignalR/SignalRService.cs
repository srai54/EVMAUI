using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace EVSwap.Mobile.SignalR;

public class SignalRService : ISignalRService
{
    private HubConnection? _hubConnection;
    private readonly ISecureStorageService _secureStorage;

    public SignalRService(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
            return;

        var token = await _secureStorage.GetAsync(Helpers.Constants.StorageKeys.AuthToken);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Helpers.Constants.ApiBaseUrl}/hubs/evswap", options =>
            {
                if (!string.IsNullOrEmpty(token))
                    options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

        await _hubConnection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public void OnBatteryUpdate(Action<BatteryModel> callback)
    {
        if (_hubConnection is not null)
            _hubConnection.On("BatteryUpdate", callback);
    }

    public void OnNotification(Action<NotificationModel> callback)
    {
        if (_hubConnection is not null)
            _hubConnection.On("Notification", callback);
    }

    public void OnSwapStatusUpdate(Action<SwapRequestModel> callback)
    {
        if (_hubConnection is not null)
            _hubConnection.On("SwapStatusUpdate", callback);
    }

    public void OnDashboardUpdate(Action<DashboardModel> callback)
    {
        if (_hubConnection is not null)
            _hubConnection.On("DashboardUpdate", callback);
    }
}
