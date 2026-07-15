using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class MaintenanceDashboardPage : ContentPage
{
    public MaintenanceDashboardPage(MaintenanceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
