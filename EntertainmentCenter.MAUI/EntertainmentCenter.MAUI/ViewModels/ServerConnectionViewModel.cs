using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Resources.Strings;
using EntertainmentCenter.Services;
using EntertainmentCenter.Messages;

namespace EntertainmentCenter.ViewModels;

public partial class ServerConnectionViewModel : ObservableObject
{
    private readonly ServerAddressManager _addressManager;

    [ObservableProperty]
    private string apiUrl;

    [ObservableProperty]
    private string statusText = "";

    [ObservableProperty]
    private string statusColor = "#B8B3AC";

    [ObservableProperty]
    private bool isChecking;

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private bool isDisconnected;

    [ObservableProperty]
    private bool showStatus;

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    private bool isBusy;

    public ServerConnectionViewModel(ServerAddressManager addressManager)
    {
        _addressManager = addressManager;
        apiUrl = _addressManager.CurrentBaseUrl;
    }

    [RelayCommand]
    private async Task Connect()
    {
        ErrorMessage = "";
        IsBusy = true;
        ShowStatus = false;

        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(ApiUrl))
            {
                ErrorMessage = AppResources.UrlRequired;
                IsBusy = false;
                return;
            }

            var trimmedUrl = ApiUrl.TrimEnd('/');
            if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri))
            {
                ErrorMessage = AppResources.InvalidUrl;
                IsBusy = false;
                return;
            }

            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                ErrorMessage = AppResources.UrlSchemeRequired;
                IsBusy = false;
                return;
            }

            // Save the URL to AddressManager immediately so it's persisted and applied
            _addressManager.CurrentBaseUrl = trimmedUrl;

            // Show checking state
            ShowStatus = true;
            IsChecking = true;
            IsConnected = false;
            IsDisconnected = false;
            StatusText = AppResources.ConnectingProgress;
            StatusColor = "#D97706";

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await client.GetAsync($"{trimmedUrl}/api/zones", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                IsChecking = false;
                IsConnected = true;
                IsDisconnected = false;
                StatusText = AppResources.ConnectedStatus;
                StatusColor = "#15803D";

                _addressManager.CurrentStatus = ServerConnectionStatus.Connected;

                await Shell.Current.DisplayAlert(
                    AppResources.Success,
                    AppResources.ServerSavedRestartMessage,
                    "OK");
            }
            else
            {
                IsChecking = false;
                IsConnected = false;
                IsDisconnected = true;
                StatusText = $"{AppResources.Error} {(int)response.StatusCode}";
                StatusColor = "#DC2626";
                ErrorMessage = string.Format(AppResources.ServerErrorMessageFormat, (int)response.StatusCode);
                
                _addressManager.CurrentStatus = ServerConnectionStatus.Disconnected;
            }
        }
        catch (HttpRequestException)
        {
            SetDisconnected(AppResources.DisconnectedStatus);
            ErrorMessage = AppResources.ServerConnectErrorPlaceholder;
            _addressManager.CurrentStatus = ServerConnectionStatus.Disconnected;
        }
        catch (TaskCanceledException)
        {
            SetDisconnected(AppResources.TimeoutStatus);
            ErrorMessage = AppResources.ServerTimeoutPlaceholder;
            _addressManager.CurrentStatus = ServerConnectionStatus.Disconnected;
        }
        catch (Exception ex)
        {
            SetDisconnected(ex.Message);
            ErrorMessage = $"{AppResources.Error}: {ex.Message}";
            _addressManager.CurrentStatus = ServerConnectionStatus.Disconnected;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetDisconnected(string text)
    {
        IsChecking = false;
        IsConnected = false;
        IsDisconnected = true;
        StatusText = text;
        StatusColor = "#DC2626";
    }
}
