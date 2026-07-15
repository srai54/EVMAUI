namespace EVSwap.Mobile.Interfaces;

public interface IConnectivityService
{
    bool IsConnected { get; }
    event EventHandler<bool>? ConnectivityChanged;
}
