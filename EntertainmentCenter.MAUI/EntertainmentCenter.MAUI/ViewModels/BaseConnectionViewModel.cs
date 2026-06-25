using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using EntertainmentCenter.Messages;
using EntertainmentCenter.Services;

namespace EntertainmentCenter.ViewModels;

public partial class BaseConnectionViewModel : ObservableObject, IRecipient<ConnectionStatusChangedMessage>
{
    protected readonly ServerAddressManager AddressManager;

    [ObservableProperty]
    private string connectionStatusText = "";

    [ObservableProperty]
    private string connectionStatusColor = "#B8B3AC"; // neutral default

    [ObservableProperty]
    private string connectionStatusBgColor = "#F1EFE8"; // light gray default

    [ObservableProperty]
    private bool showConnectionStatusBanner;

    public BaseConnectionViewModel(ServerAddressManager addressManager)
    {
        AddressManager = addressManager;
        
        // Register to receive connection status updates
        WeakReferenceMessenger.Default.Register(this);

        // Initialize with the current status
        UpdateStatus(AddressManager.CurrentStatus, AddressManager.CurrentBaseUrl);
    }

    public void Receive(ConnectionStatusChangedMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateStatus(message.Value.Status, message.Value.ExtraInfo);
        });
    }

    private void UpdateStatus(ServerConnectionStatus status, string? extraInfo)
    {
        switch (status)
        {
            case ServerConnectionStatus.Connected:
                ConnectionStatusText = $"Подключено к: {extraInfo}";
                ConnectionStatusColor = "#15803D"; // green text
                ConnectionStatusBgColor = "#DCFCE7"; // green bg
                ShowConnectionStatusBanner = false; // Hide banner when fully connected
                break;
            case ServerConnectionStatus.Searching:
                ConnectionStatusText = "Поиск сервера в локальной сети...";
                ConnectionStatusColor = "#D97706"; // warning orange text
                ConnectionStatusBgColor = "#FEF3C7"; // warning light bg
                ShowConnectionStatusBanner = true;
                break;
            case ServerConnectionStatus.Disconnected:
                ConnectionStatusText = "Нет связи с сервером. Повторное подключение...";
                ConnectionStatusColor = "#DC2626"; // danger red text
                ConnectionStatusBgColor = "#FEE2E2"; // danger light bg
                ShowConnectionStatusBanner = true;
                break;
        }
    }
}
