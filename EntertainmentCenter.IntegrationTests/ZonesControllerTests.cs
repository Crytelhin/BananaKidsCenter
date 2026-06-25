using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.IntegrationTests;

public class ZonesControllerTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public ZonesControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAllZones_ReturnsEmptyList_WhenNoZones()
    {
        var response = await _client.GetAsync("/api/zones");
        response.EnsureSuccessStatusCode();
        var zones = await response.Content.ReadFromJsonAsync<List<Zone>>();
        zones.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateZone_ReturnsCreatedZone()
    {
        var zone = new Zone { Name = "PlayStation" };
        var response = await _client.PostAsJsonAsync("/api/zones", zone);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Zone>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("PlayStation");
        created.Id.Should().BeGreaterThan(0);
        created.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllZones_ReturnsZoneWithTariffs()
    {
        var zone = new Zone { Name = "Zone With Tariff" };
        var createResponse = await _client.PostAsJsonAsync("/api/zones", zone);
        var created = await createResponse.Content.ReadFromJsonAsync<Zone>();

        var response = await _client.GetAsync("/api/zones");
        var zones = await response.Content.ReadFromJsonAsync<List<Zone>>();
        zones.Should().ContainSingle(z => z.Name == "Zone With Tariff");
        zones![0].Tariffs.Should().BeEmpty();
    }

    [Fact]
    public async Task AddTariff_ToZone()
    {
        var zone = new Zone { Name = "Zone" };
        var createResponse = await _client.PostAsJsonAsync("/api/zones", zone);
        var created = await createResponse.Content.ReadFromJsonAsync<Zone>();

        var tariff = new Tariff { Label = "1 час", Price = 50, Duration = TimeSpan.FromHours(1), ZoneId = created!.Id };
        var tariffResponse = await _client.PostAsJsonAsync($"/api/zones/{created.Id}/tariffs", tariff);
        tariffResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var getResponse = await _client.GetAsync($"/api/zones/{created.Id}");
        var zoneWithTariffs = await getResponse.Content.ReadFromJsonAsync<Zone>();
        zoneWithTariffs!.Tariffs.Should().ContainSingle(t => t.Label == "1 час");
    }

    [Fact]
    public async Task DeleteZone_SoftDeletes()
    {
        var zone = new Zone { Name = "To Delete" };
        var createResponse = await _client.PostAsJsonAsync("/api/zones", zone);
        var created = await createResponse.Content.ReadFromJsonAsync<Zone>();

        var deleteResponse = await _client.DeleteAsync($"/api/zones/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetAllZones_DoesNotReturnDeletedZone()
    {
        var zone = new Zone { Name = "Deleted Zone" };
        var createResponse = await _client.PostAsJsonAsync("/api/zones", zone);
        var created = await createResponse.Content.ReadFromJsonAsync<Zone>();
        await _client.DeleteAsync($"/api/zones/{created!.Id}");

        var response = await _client.GetAsync("/api/zones");
        var zones = await response.Content.ReadFromJsonAsync<List<Zone>>();
        zones.Should().NotContain(z => z.Name == "Deleted Zone");
    }
}