using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EntertainmentCenter.Messages;

namespace EntertainmentCenter.Services;

public interface IServerDiscoveryService
{
    void StartPassiveListener();
    void StopPassiveListener();
    Task<string?> DiscoverServerAsync(CancellationToken cancellationToken);
}

public class ServerDiscoveryService : IServerDiscoveryService
{
    private readonly ServerAddressManager _addressManager;
    private UdpClient? _passiveClient;
    private CancellationTokenSource? _passiveCts;
    private const int DiscoveryPort = 47000;
    private const string AppMarker = "EntertainmentCenter";

#if ANDROID
    private Android.Net.Wifi.WifiManager.MulticastLock? _multicastLock;
#endif

    public ServerDiscoveryService(ServerAddressManager addressManager)
    {
        _addressManager = addressManager;
    }

    public void StartPassiveListener()
    {
        if (_passiveClient != null) return;

        _passiveCts = new CancellationTokenSource();
        var token = _passiveCts.Token;

        Task.Run(async () =>
        {
            try
            {
#if ANDROID
                // Acquire MulticastLock on Android
                try
                {
                    var context = Android.App.Application.Context;
                    var wifiManager = (Android.Net.Wifi.WifiManager?)context.GetSystemService(Android.Content.Context.WifiService);
                    if (wifiManager != null)
                    {
                        _multicastLock = wifiManager.CreateMulticastLock("UdpDiscoveryLock");
                        _multicastLock?.Acquire();
                        Console.WriteLine("Android MulticastLock acquired successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to acquire MulticastLock: {ex.Message}");
                }
#endif

                _passiveClient = new UdpClient();
                _passiveClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _passiveClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

                Console.WriteLine($"Passive UDP discovery listener started on port {DiscoveryPort}");

                while (!token.IsCancellationRequested)
                {
                    var result = await _passiveClient.ReceiveAsync(token);
                    var rawData = Encoding.UTF8.GetString(result.Buffer);

                    // Skip self-sent active requests if they happen to end up here
                    if (rawData.StartsWith("DISCOVER_"))
                        continue;

                    try
                    {
                        var packet = JsonSerializer.Deserialize<DiscoveryPacket>(rawData);
                        if (packet != null && packet.AppMarker == AppMarker)
                        {
                            var senderIp = result.RemoteEndPoint.Address.ToString();
                            var newUrl = $"http://{senderIp}:{packet.ApiPort}";
                            Console.WriteLine($"Passive UDP discovery received server URL: {newUrl}");
                            
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                if (_addressManager.CurrentBaseUrl != newUrl)
                                {
                                    _addressManager.CurrentBaseUrl = newUrl;
                                }
                                _addressManager.CurrentStatus = ServerConnectionStatus.Connected;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing discovery packet: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Passive UDP discovery listener cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in passive UDP discovery listener: {ex}");
            }
            finally
            {
#if ANDROID
                try
                {
                    if (_multicastLock != null && _multicastLock.IsHeld)
                    {
                        _multicastLock.Release();
                        Console.WriteLine("Android MulticastLock released.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to release MulticastLock: {ex.Message}");
                }
                _multicastLock = null;
#endif
                _passiveClient?.Dispose();
                _passiveClient = null;
            }
        }, token);
    }

    public void StopPassiveListener()
    {
        _passiveCts?.Cancel();
        _passiveCts = null;

        _passiveClient?.Dispose();
        _passiveClient = null;
    }

    public async Task<string?> DiscoverServerAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting active server discovery query...");
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _addressManager.CurrentStatus = ServerConnectionStatus.Searching;
        });

        using var activeClient = new UdpClient();
        activeClient.EnableBroadcast = true;
        activeClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        // Bind to ephemeral port to receive responses
        activeClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

        var requestText = $"DISCOVER_{AppMarker}_SERVER";
        var requestData = Encoding.UTF8.GetBytes(requestText);

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                Console.WriteLine($"Sending broadcast active discovery probe, attempt {attempt}/3...");
                await activeClient.SendAsync(
                    requestData, 
                    requestData.Length, 
                    new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

                // Wait for response with timeout (1.5 seconds)
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(1500);

                var receiveTask = activeClient.ReceiveAsync(cts.Token);
                var response = await receiveTask;
                var rawResponse = Encoding.UTF8.GetString(response.Buffer);

                var packet = JsonSerializer.Deserialize<DiscoveryPacket>(rawResponse);
                if (packet != null && packet.AppMarker == AppMarker)
                {
                    var senderIp = response.RemoteEndPoint.Address.ToString();
                    var newUrl = $"http://{senderIp}:{packet.ApiPort}";
                    Console.WriteLine($"Active discovery succeeded! Discovered URL: {newUrl}");

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _addressManager.CurrentBaseUrl = newUrl;
                        _addressManager.CurrentStatus = ServerConnectionStatus.Connected;
                    });

                    return newUrl;
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout or cancellation token triggered
                Console.WriteLine($"Active discovery probe {attempt}/3 timed out or was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Active discovery probe {attempt}/3 failed: {ex.Message}");
            }
        }

        Console.WriteLine("Active discovery failed. No server found.");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _addressManager.CurrentStatus = ServerConnectionStatus.Disconnected;
        });

        return null;
    }

    private class DiscoveryPacket
    {
        public string AppMarker { get; set; } = "";
        public string ServerIp { get; set; } = "";
        public int ApiPort { get; set; }
        public string BaseUrl { get; set; } = "";
    }
}
