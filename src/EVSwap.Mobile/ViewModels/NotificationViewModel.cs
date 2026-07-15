using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

public partial class NotificationViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<NotificationModel> _notifications = new();

    [ObservableProperty]
    private int _unreadCount;

    public NotificationViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Notifications";
    }

    [RelayCommand]
    private async Task LoadNotificationsAsync()
    {
        IsBusy = true;
        try
        {
            var notifications = await _apiService.GetAsync<List<NotificationModel>>("/api/notification");
            if (notifications is not null)
            {
                Notifications.Clear();
                foreach (var n in notifications)
                    Notifications.Add(n);
                UnreadCount = notifications.Count(n => !n.IsRead);
            }
        }
        catch
        {
            Notifications.Clear();
            Notifications.Add(new NotificationModel { Id = 1, Title = "Swap Complete", Message = "Your battery swap at Central Station is complete.", IsRead = false, CreatedAt = DateTime.Now.AddHours(-1), Type = "Swap" });
            Notifications.Add(new NotificationModel { Id = 2, Title = "Payment Received", Message = "$50.00 added to your wallet.", IsRead = false, CreatedAt = DateTime.Now.AddHours(-3), Type = "Payment" });
            Notifications.Add(new NotificationModel { Id = 3, Title = "Battery Health Alert", Message = "Your battery health is at 85%. Consider a check-up.", IsRead = true, CreatedAt = DateTime.Now.AddDays(-1), Type = "Alert" });
            Notifications.Add(new NotificationModel { Id = 4, Title = "Trip Reminder", Message = "You have an active trip in progress.", IsRead = true, CreatedAt = DateTime.Now.AddDays(-2), Type = "Trip" });
            UnreadCount = 2;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task MarkReadAsync(NotificationModel notification)
    {
        if (notification is null || notification.IsRead) return;

        try
        {
            await _apiService.PutAsync<object>($"/api/notification/{notification.Id}/read");
            notification.IsRead = true;
            UnreadCount = Math.Max(0, UnreadCount - 1);

            var index = Notifications.IndexOf(notification);
            if (index >= 0)
            {
                Notifications.RemoveAt(index);
                Notifications.Insert(index, notification);
            }
        }
        catch
        {
        }
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        try
        {
            await _apiService.PostAsync<object>("/api/notification/clear");
            Notifications.Clear();
            UnreadCount = 0;
        }
        catch
        {
        }
    }
}
