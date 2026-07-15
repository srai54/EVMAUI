using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class AdminDashboardPage : ContentPage
{
    public AdminDashboardPage(AdminDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
