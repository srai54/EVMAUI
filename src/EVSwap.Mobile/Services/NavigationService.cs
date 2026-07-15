using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile.Services;

public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (parameters is not null)
            await Shell.Current.GoToAsync(route, parameters);
        else
            await Shell.Current.GoToAsync(route);
    }

    public async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
