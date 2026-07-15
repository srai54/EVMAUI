using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Models;
using EVSwap.Mobile.Views;

namespace EVSwap.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(Constants.Routes.StationDetail, typeof(StationDetailPage));
        Routing.RegisterRoute(Constants.Routes.SwapRequest, typeof(SwapRequestPage));
        Routing.RegisterRoute(Constants.Routes.QRScan, typeof(QRScanPage));
        Routing.RegisterRoute(Constants.Routes.AddMoney, typeof(AddMoneyPage));
        Routing.RegisterRoute(Constants.Routes.Settings, typeof(SettingsPage));
    }
}
