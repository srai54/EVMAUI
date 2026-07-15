namespace EVSwap.Mobile.Interfaces;

public interface INavigationService
{
    Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
}
