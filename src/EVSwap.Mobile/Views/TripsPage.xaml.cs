using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class TripsPage : ContentPage
{
    public TripsPage(TripViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
