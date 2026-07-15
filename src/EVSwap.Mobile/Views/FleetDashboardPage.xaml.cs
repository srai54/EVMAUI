using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class FleetDashboardPage : ContentPage
{
    public FleetDashboardPage(FleetViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
