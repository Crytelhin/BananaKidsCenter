using CommunityToolkit.Mvvm.Messaging.Messages;
using EntertainmentCenter.Services;

namespace EntertainmentCenter.Messages;

public enum ServerConnectionStatus
{
    Connected,
    Searching,
    Disconnected
}

public class ConnectionStatusChangedMessage : ValueChangedMessage<(ServerConnectionStatus Status, string ExtraInfo)>
{
    public ConnectionStatusChangedMessage((ServerConnectionStatus Status, string ExtraInfo) value) : base(value)
    {
    }
}
