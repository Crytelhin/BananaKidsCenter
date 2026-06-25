using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace EntertainmentCenter.API.Services;

public class UdpDiscoveryHostedService : BackgroundService
{
    private readonly ILogger<UdpDiscoveryHostedService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServer _server;

    private int _port = 47000;
    private int _intervalMinutes = 1;
    private string _appMarker = "EntertainmentCenter";
    private string _broadcastIpStr = "255.255.255.255";

    public UdpDiscoveryHostedService(
        ILogger<UdpDiscoveryHostedService> logger,
        IConfiguration configuration,
        IServer server)
    {
        _logger = logger;
        _configuration = configuration;
        _server = server;

        ConfigureSettings();
    }

    private void ConfigureSettings()
    {
        var section = _configuration.GetSection("DiscoverySettings");
        if (section.Exists())
        {
            if (int.TryParse(section["Port"], out var port))
                _port = port;

            if (int.TryParse(section["IntervalMinutes"], out var interval))
                _intervalMinutes = interval;

            if (!string.IsNullOrEmpty(section["AppMarker"]))
                _appMarker = section["AppMarker"]!;

            if (!string.IsNullOrEmpty(section["BroadcastAddress"]))
                _broadcastIpStr = section["BroadcastAddress"]!;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UdpDiscoveryHostedService started. Discovery Port: {Port}, Interval: {Interval}m", _port, _intervalMinutes);

        // Run both listener and monitor concurrently
        var listenerTask = RunDiscoveryListenerAsync(stoppingToken);
        var monitorTask = RunIpMonitorLoopAsync(stoppingToken);

        await Task.WhenAll(listenerTask, monitorTask);
    }

    private async Task RunDiscoveryListenerAsync(CancellationToken stoppingToken)
    {
        using var udpClient = new UdpClient();
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _port));

        _logger.LogInformation("UDP Discovery Listener listening on port {Port}", _port);

        var expectedRequest = $"DISCOVER_{_appMarker}_SERVER";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(stoppingToken);
                var requestStr = Encoding.UTF8.GetString(result.Buffer);

                if (requestStr == expectedRequest)
                {
                    _logger.LogInformation("Received active discovery pull request from {RemoteEndPoint}", result.RemoteEndPoint);

                    var netAddresses = GetNetworkAddresses();
                    if (netAddresses.Any())
                    {
                        var apiPort = GetServerPort();

                        // Select the local IP that matches the subnet of the client, otherwise default to first
                        var localIp = netAddresses[0].LocalIp;
                        foreach (var addr in netAddresses)
                        {
                            if (IsInSameSubnet(addr.LocalIp, result.RemoteEndPoint.Address, addr.BroadcastIp))
                            {
                                localIp = addr.LocalIp;
                                break;
                            }
                        }

                        var packet = new DiscoveryPacket
                        {
                            AppMarker = _appMarker,
                            ServerIp = localIp.ToString(),
                            ApiPort = apiPort,
                            BaseUrl = $"http://{localIp}:{apiPort}"
                        };

                        var json = JsonSerializer.Serialize(packet);
                        var responseBytes = Encoding.UTF8.GetBytes(json);

                        await udpClient.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                        _logger.LogInformation("Sent active discovery response to {RemoteEndPoint}: {BaseUrl}", result.RemoteEndPoint, packet.BaseUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Cannot reply to discovery: no active network interfaces found.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UDP Discovery Listener loop");
            }
        }
    }

    private async Task RunIpMonitorLoopAsync(CancellationToken stoppingToken)
    {
        // Give the web host a moment to start and bind ports before checking
        await Task.Delay(3000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var netAddresses = GetNetworkAddresses();
                if (netAddresses.Any())
                {
                    var currentIps = netAddresses.Select(n => n.LocalIp.ToString()).OrderBy(ip => ip).ToList();
                    var savedIps = await GetSavedIpsAsync();

                    if (!currentIps.SequenceEqual(savedIps))
                    {
                        _logger.LogWarning("Server IPs changed. Old: {Saved}, New: {Current}. Broadcasting update...",
                            string.Join(", ", savedIps), string.Join(", ", currentIps));

                        await SaveIpsAsync(currentIps);

                        foreach (var addr in netAddresses)
                        {
                            await BroadcastNewIpAsync(addr.LocalIp, addr.BroadcastIp, stoppingToken);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No active local IP address found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IP monitor loop");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task BroadcastNewIpAsync(IPAddress localIp, IPAddress broadcastIp, CancellationToken stoppingToken)
    {
        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        var apiPort = GetServerPort();
        var packet = new DiscoveryPacket
        {
            AppMarker = _appMarker,
            ServerIp = localIp.ToString(),
            ApiPort = apiPort,
            BaseUrl = $"http://{localIp}:{apiPort}"
        };

        var json = JsonSerializer.Serialize(packet);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Fallback to configured broadcast IP if specified
        var targetEndpoint = new IPEndPoint(broadcastIp, _port);
        if (IPAddress.TryParse(_broadcastIpStr, out var configBroadcastIp))
        {
            targetEndpoint = new IPEndPoint(configBroadcastIp, _port);
        }

        _logger.LogInformation("Broadcasting server IP to {Endpoint}", targetEndpoint);

        for (int i = 1; i <= 3; i++)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await udpClient.SendAsync(bytes, bytes.Length, targetEndpoint);
                _logger.LogInformation("IP change broadcast push sent ({Attempt}/3) on {Endpoint}", i, targetEndpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send IP broadcast push attempt {Attempt} on {Endpoint}", i, targetEndpoint);
            }

            await Task.Delay(500, stoppingToken);
        }
    }

    private int GetServerPort()
    {
        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses != null && addresses.Any())
        {
            foreach (var address in addresses)
            {
                if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
                {
                    return uri.Port;
                }
            }
        }

        var urls = _configuration["Urls"] ?? _configuration["Microsoft.AspNetCore.Http.Endpoints"];
        if (!string.IsNullOrEmpty(urls))
        {
            var parts = urls.Split(';');
            foreach (var part in parts)
            {
                if (Uri.TryCreate(part, UriKind.Absolute, out var uri))
                {
                    return uri.Port;
                }
            }
        }

        return 5000;
    }

    public static List<(IPAddress LocalIp, IPAddress BroadcastIp)> GetNetworkAddresses()
    {
        var list = new List<(IPAddress LocalIp, IPAddress BroadcastIp)>();

        var allInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(i => i.OperationalStatus == OperationalStatus.Up &&
                        i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToList();

        var physicalInterfaces = allInterfaces
            .Where(i =>
            {
                var desc = i.Description.ToLower();
                var name = i.Name.ToLower();
                return !desc.Contains("virtual") && !name.Contains("virtual") &&
                       !desc.Contains("vpn") && !name.Contains("vpn") &&
                       !desc.Contains("hamachi") && !name.Contains("hamachi") &&
                       !desc.Contains("virtualbox") && !name.Contains("virtualbox") &&
                       !desc.Contains("vmware") && !name.Contains("vmware") &&
                       !desc.Contains("wsl") && !name.Contains("wsl") &&
                       !desc.Contains("vbox") && !name.Contains("vbox") &&
                       !desc.Contains("veth") && !name.Contains("veth") &&
                       !desc.Contains("vethernet") && !name.Contains("vethernet");
            })
            .ToList();

        var targetInterfaces = physicalInterfaces.Any() ? physicalInterfaces : allInterfaces;

        foreach (var ni in targetInterfaces)
        {
            var props = ni.GetIPProperties();
            foreach (var addr in props.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var localIp = addr.Address;
                    var mask = addr.IPv4Mask;
                    if (mask == null || mask.Equals(IPAddress.Any))
                    {
                        list.Add((localIp, IPAddress.Broadcast));
                        continue;
                    }

                    var ipBytes = localIp.GetAddressBytes();
                    var maskBytes = mask.GetAddressBytes();
                    var broadcastBytes = new byte[4];
                    for (int i = 0; i < 4; i++)
                    {
                        broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                    }
                    list.Add((localIp, new IPAddress(broadcastBytes)));
                }
            }
        }
        return list;
    }

    private static bool IsInSameSubnet(IPAddress ip1, IPAddress ip2, IPAddress broadcastIp)
    {
        var ip1Bytes = ip1.GetAddressBytes();
        var ip2Bytes = ip2.GetAddressBytes();

        var maskBytes = new byte[4];
        var broadBytes = broadcastIp.GetAddressBytes();
        for (int i = 0; i < 4; i++)
        {
            maskBytes[i] = (byte)~(broadBytes[i] ^ ip1Bytes[i]);
        }

        for (int i = 0; i < 4; i++)
        {
            if ((ip1Bytes[i] & maskBytes[i]) != (ip2Bytes[i] & maskBytes[i]))
                return false;
        }
        return true;
    }

    private string GetStorageFilePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EntertainmentCenter");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "server_ips.json");
    }

    private async Task<List<string>> GetSavedIpsAsync()
    {
        var path = GetStorageFilePath();
        if (!File.Exists(path)) return new List<string>();

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var data = JsonSerializer.Deserialize<IpStorageData>(json);
            return data?.LastKnownIps ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read saved server IPs");
            return new List<string>();
        }
    }

    private async Task SaveIpsAsync(List<string> ips)
    {
        var path = GetStorageFilePath();
        try
        {
            var data = new IpStorageData { LastKnownIps = ips };
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save server IPs");
        }
    }

    private class IpStorageData
    {
        public List<string> LastKnownIps { get; set; } = new();
    }

    private class DiscoveryPacket
    {
        public string AppMarker { get; set; } = "";
        public string ServerIp { get; set; } = "";
        public int ApiPort { get; set; }
        public string BaseUrl { get; set; } = "";
    }
}
