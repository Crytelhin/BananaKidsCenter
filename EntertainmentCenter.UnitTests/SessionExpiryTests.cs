using Xunit;
using EntertainmentCenter.API.Models;
using EntertainmentCenter.API.Services;
using FluentAssertions;

namespace EntertainmentCenter.UnitTests;

public class SessionExpiryTests
{
    [Fact]
    public async Task ExpiresAtInFuture_SessionIsActive()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var client = new Client { Id = 1, FullName = "Test", CardCode = "CARD001" };
        var zone = new Zone { Id = 1, Name = "Zone" };
        var tariff = new Tariff { Id = 1, Label = "1h", Price = 50, Duration = TimeSpan.FromHours(1), ZoneId = 1 };
        var session = new Session
        {
            ClientId = 1,
            Client = client,
            TariffId = 1,
            Tariff = tariff,
            FinalPrice = 50,
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = DateTime.UtcNow,
            DurationMinutes = 60,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsActive = true
        };
        context.Clients.Add(client);
        context.Zones.Add(zone);
        context.Tariffs.Add(tariff);
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new SessionService(context);

        // Act
        var activeSessions = await service.GetAllActiveAsync();

        // Assert
        activeSessions.Should().ContainSingle();
    }

    [Fact]
    public async Task ExpiresAtInPast_NotReturnedByGetAllActive()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var client = new Client { Id = 1, FullName = "Test", CardCode = "CARD001" };
        var zone = new Zone { Id = 1, Name = "Zone" };
        var tariff = new Tariff { Id = 1, Label = "1h", Price = 50, Duration = TimeSpan.FromHours(1), ZoneId = 1 };
        var expiredSession = new Session
        {
            ClientId = 1,
            Client = client,
            TariffId = 1,
            Tariff = tariff,
            FinalPrice = 50,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ActivatedAt = DateTime.UtcNow.AddHours(-2),
            DurationMinutes = 60,
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            IsActive = true
        };
        context.Clients.Add(client);
        context.Zones.Add(zone);
        context.Tariffs.Add(tariff);
        context.Sessions.Add(expiredSession);
        await context.SaveChangesAsync();

        var service = new SessionService(context);

        // Act
        var activeSessions = await service.GetAllActiveAsync();

        // Assert
        activeSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task StartSession_CreatesPendingSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var client = new Client { Id = 1, FullName = "Test", CardCode = "CARD001" };
        var zone = new Zone { Id = 1, Name = "Zone" };
        var tariff = new Tariff { Id = 1, Label = "2h", Price = 80, Duration = TimeSpan.FromHours(2), ZoneId = 1 };
        context.Clients.Add(client);
        context.Zones.Add(zone);
        context.Tariffs.Add(tariff);
        await context.SaveChangesAsync();

        var service = new SessionService(context);

        // Act
        var session = await service.StartSessionAsync(1, 1, null);

        // Assert
        session.IsActive.Should().BeFalse();
        session.ActivatedAt.Should().BeNull();
        session.DurationMinutes.Should().Be(120);
    }

    [Fact]
    public async Task ActivateSession_StartsTimer()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var client = new Client { Id = 1, FullName = "Test", CardCode = "CARD001" };
        var zone = new Zone { Id = 1, Name = "Zone" };
        var tariff = new Tariff { Id = 1, Label = "2h", Price = 80, Duration = TimeSpan.FromHours(2), ZoneId = 1 };
        context.Clients.Add(client);
        context.Zones.Add(zone);
        context.Tariffs.Add(tariff);
        await context.SaveChangesAsync();

        var service = new SessionService(context);
        await service.StartSessionAsync(1, 1, null);

        // Act
        var activated = await service.ActivateSessionAsync("CARD001");

        // Assert
        activated.Should().NotBeNull();
        activated!.IsActive.Should().BeTrue();
        activated.ActivatedAt.Should().NotBeNull();
        activated.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(2), TimeSpan.FromSeconds(5));
    }
}