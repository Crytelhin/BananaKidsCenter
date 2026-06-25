using System.Net.Http.Json;
using Xunit;
using FluentAssertions;

namespace EntertainmentCenter.IntegrationTests;

public class AdminControllerTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public AdminControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task VerifyPin_CorrectPin_ReturnsTrue()
    {
        var request = new { Pin = "1234" };
        var response = await _client.PostAsJsonAsync("/api/admin/verify-pin", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<bool>();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPin_WrongPin_ReturnsFalse()
    {
        var request = new { Pin = "wrong" };
        var response = await _client.PostAsJsonAsync("/api/admin/verify-pin", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<bool>();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePin_UpdatesPinAndOldPinNoLongerWorks()
    {
        var changeRequest = new { NewPin = "9999" };
        var changeResponse = await _client.PostAsJsonAsync("/api/admin/change-pin", changeRequest);
        changeResponse.EnsureSuccessStatusCode();
        var changeResult = await changeResponse.Content.ReadFromJsonAsync<bool>();
        changeResult.Should().BeTrue();

        // Old pin no longer works
        var verifyOld = await _client.PostAsJsonAsync("/api/admin/verify-pin", new { Pin = "1234" });
        var oldResult = await verifyOld.Content.ReadFromJsonAsync<bool>();
        oldResult.Should().BeFalse();

        // New pin works
        var verifyNew = await _client.PostAsJsonAsync("/api/admin/verify-pin", new { Pin = "9999" });
        var newResult = await verifyNew.Content.ReadFromJsonAsync<bool>();
        newResult.Should().BeTrue();
    }
}