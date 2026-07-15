using CommunityToolkit.Maui;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Services;
using EVSwap.Mobile.ViewModels;
using EVSwap.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace EVSwap.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IApiService, ApiService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ForgotPasswordViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<StationViewModel>();
        builder.Services.AddTransient<StationDetailViewModel>();
        builder.Services.AddTransient<BatterySwapViewModel>();
        builder.Services.AddTransient<SwapRequestViewModel>();
        builder.Services.AddTransient<QRScanViewModel>();
        builder.Services.AddTransient<TripViewModel>();
        builder.Services.AddTransient<WalletViewModel>();
        builder.Services.AddTransient<AddMoneyViewModel>();
        builder.Services.AddTransient<NotificationViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<AdminDashboardViewModel>();
        builder.Services.AddTransient<UserManagementViewModel>();
        builder.Services.AddTransient<FleetViewModel>();
        builder.Services.AddTransient<MaintenanceViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<StationsPage>();
        builder.Services.AddTransient<StationDetailPage>();
        builder.Services.AddTransient<BatterySwapPage>();
        builder.Services.AddTransient<SwapRequestPage>();
        builder.Services.AddTransient<QRScanPage>();
        builder.Services.AddTransient<TripsPage>();
        builder.Services.AddTransient<WalletPage>();
        builder.Services.AddTransient<AddMoneyPage>();
        builder.Services.AddTransient<NotificationsPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AdminDashboardPage>();
        builder.Services.AddTransient<UserManagementPage>();
        builder.Services.AddTransient<FleetDashboardPage>();
        builder.Services.AddTransient<MaintenanceDashboardPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}