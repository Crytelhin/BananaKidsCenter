using CommunityToolkit.Mvvm.Messaging;
using EntertainmentCenter.Messages;

namespace EntertainmentCenter.Services;

public class ServerAddressManager
{
    private string _currentBaseUrl;
    private ServerConnectionStatus _currentStatus;

    public ServerAddressManager()
    {
        _currentBaseUrl = Preferences.Get("ApiBaseUrl", ApiConstants.BaseUrl).TrimEnd('/');
        _currentStatus = ServerConnectionStatus.Connected; // Assume connected initially, discovery will update
    }

    public string CurrentBaseUrl
    {
        get => _currentBaseUrl;
        set
        {
            var cleanUrl = value?.TrimEnd('/') ?? "";
            if (_currentBaseUrl != cleanUrl)
            {
                _currentBaseUrl = cleanUrl;
                Preferences.Set("ApiBaseUrl", cleanUrl);
                NotifyStatusChanged(_currentStatus, cleanUrl);
            }
        }
    }

    public ServerConnectionStatus CurrentStatus
    {
        get => _currentStatus;
        set
        {
            if (_currentStatus != value)
            {
                _currentStatus = value;
                NotifyStatusChanged(value, _currentBaseUrl);
            }
        }
    }

    public void NotifyStatusChanged(ServerConnectionStatus status, string extraInfo)
    {
        WeakReferenceMessenger.Default.Send(new ConnectionStatusChangedMessage((status, extraInfo)));
    }
}
