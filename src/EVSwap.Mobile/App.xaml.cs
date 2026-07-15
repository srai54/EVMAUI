using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile;

public partial class App : Application
{
    private readonly ISecureStorageService _secureStorage;

    public App(ISecureStorageService secureStorage)
    {
        InitializeComponent();
        _secureStorage = secureStorage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        var window = new Window(shell);

        shell.Loaded += async (s, e) =>
        {
            var token = await _secureStorage.GetAsync(Constants.StorageKeys.AuthToken);
            if (!string.IsNullOrEmpty(token))
                await shell.GoToAsync($"//{Constants.Routes.Dashboard}");
            else
                await shell.GoToAsync($"//{Constants.Routes.Login}");
        };

        return window;
    }
}
