using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class QRScanPage : ContentPage
{
    public QRScanPage(QRScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
