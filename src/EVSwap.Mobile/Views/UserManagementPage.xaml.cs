using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class UserManagementPage : ContentPage
{
    public UserManagementPage(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
