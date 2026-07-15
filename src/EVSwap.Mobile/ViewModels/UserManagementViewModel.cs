using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

public partial class UserManagementViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<UserModel> _users = new();

    [ObservableProperty]
    private string _selectedRole = "All";

    public List<string> Roles { get; } = new() { "All", "Rider", "Admin", "Operator" };

    public UserManagementViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "User Management";
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        IsBusy = true;
        try
        {
            var users = await _apiService.GetAsync<List<UserModel>>("/api/admin/users");
            if (users is not null)
            {
                Users.Clear();
                foreach (var u in users)
                    Users.Add(u);
            }
        }
        catch
        {
        }
        finally
        {
            IsBusy = false;
        }
    }
}
