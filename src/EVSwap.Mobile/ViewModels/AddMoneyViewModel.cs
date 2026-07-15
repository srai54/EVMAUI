using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Validators;

namespace EVSwap.Mobile.ViewModels;

public partial class AddMoneyViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string _amount = string.Empty;

    [ObservableProperty]
    private string _selectedPaymentMethod = "Card";

    public List<string> PaymentMethods { get; } = new() { "Card", "UPI", "Net Banking" };

    public AddMoneyViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Add Money";
    }

    [RelayCommand]
    private async Task AddMoneyAsync()
    {
        if (!decimal.TryParse(Amount, out var amount) || !Validators.Validators.IsValidAmount(amount))
        {
            await ShowAlertAsync("Validation", "Please enter a valid amount greater than 0.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiService.PostAsync<object>("/api/wallet/add", new
            {
                Amount = amount,
                PaymentMethod = SelectedPaymentMethod
            });

            await ShowAlertAsync("Success", $"${amount:F2} added to your wallet.");
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
