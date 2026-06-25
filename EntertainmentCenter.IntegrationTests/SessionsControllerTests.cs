using System.Net.Http.Json;
using Xunit;
using FluentAssertions;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.IntegrationTests;

public class SessionsControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public SessionsControllerTests(ApiFactory factory)
    {
        _client = factory.CreateTestClient();
    }

    private static int _cardCounter;

    private async Task<(int clientId, int tariffId, string cardCode)> SetupTestData()
    {
        var cardCode = $"CARD_SESS_{Interlocked.Increment(ref _cardCounter)}";
        var client = new Client { FullName = "Test Client", Phone = "1", CardCode = cardCode };
        var clientResponse = await _client.PostAsJsonAsync("/api/clients", client);
        var createdClient = await clientResponse.Content.ReadFromJsonAsync<Client>();

        var zone = new Zone { Name = "Test Zone" };
        var zoneResponse = await _client.PostAsJsonAsync("/api/zones", zone);
        var createdZone = await zoneResponse.Content.ReadFromJsonAsync<Zone>();

        var tariff = new Tariff { Label = "2h", Price = 100, Duration = TimeSpan.FromHours(2), ZoneId = createdZone!.Id };
        var tariffResponse = await _client.PostAsJsonAsync($"/api/zones/{createdZone.Id}/tariffs", tariff);
        var createdTariff = await tariffResponse.Content.ReadFromJsonAsync<Tariff>();

        return (createdClient!.Id, createdTariff!.Id, cardCode);
    }

    [Fact]
    public async Task StartSession_CreatesPendingSession()
    {
        var (clientId, tariffId, _) = await SetupTestData();

        var request = new { ClientId = clientId, TariffId = tariffId, PromotionId = (int?)null };
        var response = await _client.PostAsJsonAsync("/api/sessions", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var session = await response.Content.ReadFromJsonAsync<Session>();
        session.Should().NotBeNull();
        session!.FinalPrice.Should().Be(100);
        session.IsActive.Should().BeFalse();
        session.ActivatedAt.Should().BeNull();
        session.DurationMinutes.Should().Be(120);
    }

    [Fact]
    public async Task StartSession_WithPromotion_DiscountsCorrectly()
    {
        var (clientId, tariffId, _) = await SetupTestData();

        var promo = new Promotion { Name = "50%", DiscountType = DiscountType.Percent, DiscountValue = 50, IsActive = true };
        var promoResponse = await _client.PostAsJsonAsync("/api/promotions", promo);
        var createdPromo = await promoResponse.Content.ReadFromJsonAsync<Promotion>();

        var request = new { ClientId = clientId, TariffId = tariffId, PromotionId = createdPromo!.Id };
        var response = await _client.PostAsJsonAsync("/api/sessions", request);
        var session = await response.Content.ReadFromJsonAsync<Session>();
        session!.FinalPrice.Should().Be(50);
    }

    [Fact]
    public async Task GetActive_ReturnsPendingAndActivatedSessions()
    {
        var (clientId, tariffId, cardCode) = await SetupTestData();

        var request = new { ClientId = clientId, TariffId = tariffId, PromotionId = (int?)null };
        await _client.PostAsJsonAsync("/api/sessions", request);

        // Before activation — pending session should appear (created today, not yet activated)
        var responseBefore = await _client.GetAsync("/api/sessions/active");
        var sessionsBefore = await responseBefore.Content.ReadFromJsonAsync<List<Session>>();
        sessionsBefore!.Should().Contain(s => s.ClientId == clientId && s.ActivatedAt == null);

        // Activate it
        await _client.GetAsync($"/api/sessions/check/{cardCode}");

        // After activation — active list should still contain it as activated
        var responseAfter = await _client.GetAsync("/api/sessions/active");
        responseAfter.EnsureSuccessStatusCode();
        var sessionsAfter = await responseAfter.Content.ReadFromJsonAsync<List<Session>>();
        sessionsAfter!.Should().Contain(s => s.ClientId == clientId && s.IsActive && s.ActivatedAt != null);
    }

    [Fact]
    public async Task CheckEntry_ValidCard_ActivatesSession()
    {
        var (clientId, tariffId, cardCode) = await SetupTestData();

        var request = new { ClientId = clientId, TariffId = tariffId, PromotionId = (int?)null };
        await _client.PostAsJsonAsync("/api/sessions", request);

        // First check -> activates
        var response = await _client.GetAsync($"/api/sessions/check/{cardCode}");
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<Session>();
        session.Should().NotBeNull();
        session!.IsActive.Should().BeTrue();
        session.ActivatedAt.Should().NotBeNull();
        session.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(2), TimeSpan.FromSeconds(10));

        // Second check -> stays active and same ActivatedAt
        var response2 = await _client.GetAsync($"/api/sessions/check/{cardCode}");
        response2.EnsureSuccessStatusCode();
        var session2 = await response2.Content.ReadFromJsonAsync<Session>();
        session2!.ActivatedAt.Should().Be(session.ActivatedAt);
    }

    [Fact]
    public async Task CheckEntry_UnknownCard_Returns404()
    {
        var response = await _client.GetAsync("/api/sessions/check/UNKNOWN_CARD");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckEntry_ExpiredSession_Returns404()
    {
        var response = await _client.GetAsync("/api/sessions/check/NO_SESSION_CARD");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}

