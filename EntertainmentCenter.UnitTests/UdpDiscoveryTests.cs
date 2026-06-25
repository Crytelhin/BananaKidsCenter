using System.Linq;
using Xunit;
using FluentAssertions;
using System.Net;
using EntertainmentCenter.API.Services;

namespace EntertainmentCenter.UnitTests;

public class UdpDiscoveryTests
{
    [Fact]
    public void GetNetworkAddresses_ShouldNotThrow_AndReturnValidAddresses()
    {
        // Act
        var results = UdpDiscoveryHostedService.GetNetworkAddresses();

        // Assert — returns a list, may be empty if no network interfaces found
        results.Should().NotBeNull();

        if (results.Any())
        {
            foreach (var (localIp, broadcastIp) in results)
            {
                // Local IP should be IPv4
                localIp.AddressFamily.Should().Be(System.Net.Sockets.AddressFamily.InterNetwork);
                // Broadcast IP should be IPv4
                broadcastIp.AddressFamily.Should().Be(System.Net.Sockets.AddressFamily.InterNetwork);
                // Local IP should not be loopback
                IPAddress.IsLoopback(localIp).Should().BeFalse();
            }
        }
        // else: no active network interface found — valid in some environments (e.g., CI)
    }
}
