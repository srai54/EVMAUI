using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class WalletPage : ContentPage
{
    public WalletPage(WalletViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
