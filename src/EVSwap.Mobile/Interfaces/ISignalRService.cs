using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.Interfaces;

public interface ISignalRService
{
    Task ConnectAsync();
    Task DisconnectAsync();
    void OnBatteryUpdate(Action<BatteryModel> callback);
    void OnNotification(Action<NotificationModel> callback);
    void OnSwapStatusUpdate(Action<SwapRequestModel> callback);
    void OnDashboardUpdate(Action<DashboardModel> callback);
}
