using EVSwap.Mobile.ViewModels;

namespace EVSwap.Mobile.Views;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage(NotificationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
