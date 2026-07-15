using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class SwapRequestPage : ContentPage
{
    public SwapRequestPage(SwapRequestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
