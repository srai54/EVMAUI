using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class AddMoneyPage : ContentPage
{
    public AddMoneyPage(AddMoneyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
