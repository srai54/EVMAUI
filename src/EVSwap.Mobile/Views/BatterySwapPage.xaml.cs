using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class BatterySwapPage : ContentPage
{
    public BatterySwapPage(BatterySwapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
