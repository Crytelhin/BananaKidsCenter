using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.IntegrationTests;

public class ClientsControllerTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public ClientsControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateClient_ReturnsCreated()
    {
        var client = new Client { FullName = "Иван Петров", Phone = "+37379123456", CardCode = "CARD001" };
        var response = await _client.PostAsJsonAsync("/api/clients", client);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Client>();
        created.Should().NotBeNull();
        created!.FullName.Should().Be("Иван Петров");
        created.CardCode.Should().Be("CARD001");
        created.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByCardCode_ReturnsCorrectClient()
    {
        var client = new Client { FullName = "Петр Сидоров", Phone = "+37379123456", CardCode = "CARD999" };
        await _client.PostAsJsonAsync("/api/clients", client);

        var response = await _client.GetAsync("/api/clients/card/CARD999");
        response.EnsureSuccessStatusCode();
        var found = await response.Content.ReadFromJsonAsync<Client>();
        found!.FullName.Should().Be("Петр Сидоров");
    }

    [Fact]
    public async Task GetByCardCode_UnknownCode_Returns404()
    {
        var response = await _client.GetAsync("/api/clients/card/UNKNOWN");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_ReturnsMatchingClients()
    {
        await _client.PostAsJsonAsync("/api/clients", new Client { FullName = "Иванов", Phone = "1", CardCode = "C1" });
        await _client.PostAsJsonAsync("/api/clients", new Client { FullName = "Петров", Phone = "2", CardCode = "C2" });

        var response = await _client.GetAsync("/api/clients/search?q=Ива");
        response.EnsureSuccessStatusCode();
        var clients = await response.Content.ReadFromJsonAsync<List<Client>>();
        clients.Should().ContainSingle(c => c.FullName == "Иванов");
        clients.Should().NotContain(c => c.FullName == "Петров");
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/clients/search?q=xxxx_nonexistent");
        response.EnsureSuccessStatusCode();
        var clients = await response.Content.ReadFromJsonAsync<List<Client>>();
        clients.Should().BeEmpty();
    }
}