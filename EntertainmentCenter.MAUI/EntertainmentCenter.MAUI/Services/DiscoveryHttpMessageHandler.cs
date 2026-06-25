using System.Net.Sockets;
using EntertainmentCenter.Messages;

namespace EntertainmentCenter.Services;

public class DiscoveryHttpMessageHandler : DelegatingHandler
{
    private readonly ServerAddressManager _addressManager;
    private readonly IServerDiscoveryService _discoveryService;

    public DiscoveryHttpMessageHandler(ServerAddressManager addressManager, IServerDiscoveryService discoveryService)
    {
        _addressManager = addressManager;
        _discoveryService = discoveryService;
        
        // Use PlatformHttpMessageHandler as the inner handler by default if not set
#if ANDROID
        InnerHandler = new Xamarin.Android.Net.AndroidMessageHandler();
#else
        InnerHandler = new HttpClientHandler();
#endif
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int maxRetries = 2;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Re-write the request URI based on current base URL from manager
                var newUriBuilder = new UriBuilder(_addressManager.CurrentBaseUrl)
                {
                    Path = request.RequestUri?.AbsolutePath ?? "",
                    Query = request.RequestUri?.Query ?? ""
                };
                request.RequestUri = newUriBuilder.Uri;

                Console.WriteLine($"HttpClient sending request to: {request.RequestUri}");
                var response = await base.SendAsync(request, cancellationToken);

                // Successfully contacted the server, update status to Connected
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_addressManager.CurrentStatus != ServerConnectionStatus.Connected)
                    {
                        _addressManager.CurrentStatus = ServerConnectionStatus.Connected;
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error encountered during request: {ex.GetType().Name} - {ex.Message}");

                if (attempt == maxRetries - 1)
                {
                    // Out of retries, mark as disconnected and throw
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _addressManager.CurrentStatus = ServerConnectionStatus.Disconnected;
                    });
                    throw;
                }

                Console.WriteLine("Attempting to discover server...");
                var discoveredUrl = await _discoveryService.DiscoverServerAsync(CancellationToken.None);
                if (discoveredUrl == null)
                {
                    // Discovery failed, cannot proceed. Mark as disconnected and throw.
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _addressManager.CurrentStatus = ServerConnectionStatus.Disconnected;
                    });
                    throw;
                }
            }
        }

        throw new HttpRequestException("HTTP request failed after server discovery.");
    }
}
