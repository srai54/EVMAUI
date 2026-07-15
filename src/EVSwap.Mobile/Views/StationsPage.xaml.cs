using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class StationsPage : ContentPage
{
    public StationsPage(StationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
