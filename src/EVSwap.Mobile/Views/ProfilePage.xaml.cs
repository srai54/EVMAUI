using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
