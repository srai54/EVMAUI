using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class StationDetailPage : ContentPage
{
    public StationDetailPage(StationDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnMapTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is StationDetailViewModel vm && vm.Station is not null)
        {
            var url = $"https://maps.google.com/?q={vm.Station.Latitude},{vm.Station.Longitude}";
            Launcher.OpenAsync(new Uri(url)).FireAndForget();
        }
    }
}
