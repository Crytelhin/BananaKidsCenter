using Xunit;
using EntertainmentCenter.API.Models;
using EntertainmentCenter.API.Services;
using FluentAssertions;

namespace EntertainmentCenter.UnitTests;

public class SessionServicePriceTests
{
    [Fact]
    public async Task NoPromotion_FinalPrice_Equals_TariffPrice()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var client = new Client { Id = 1, FullName = "Test", CardCode = "CARD001" };
        var zone = new Zone { Id = 1, Name = "Test Zone" };
        var tariff = new Tariff { Id = 1, Label = "1 час", Price = 100, Duration = TimeSpan.FromHours(1), ZoneId = 1 };
        context.Clients.Add(client);
        context.Zones.Add(zone);
        context.Tariffs.Add(tariff);
        await context.SaveChangesAsync();

        var service = new SessionService(context);

        // Act
        var session = await service.StartSessionAsync(1, 1, null);

        // Assert
        session.FinalPrice.Should().Be(100);
    }

    [Fact]
    public async Task PercentDiscount50_FinalPrice_IsHalf()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var client = new Client { Id = 1, FullName = "Test", CardCode = "CARD001" };
        var zone = new Zone { Id = 1, Name = "Test Zone" };
        var tariff = new Tariff { Id = 1, Label = "1 час", Price = 100, Duration = TimeSpan.FromHours(1), ZoneId = 1 };
        var promo = new Promotion { Id = 1, Name = "50% скидка", DiscountType = DiscountType.Percent, DiscountValue = 50, IsActive = true };
        context.Clients.Add(client);
        context.Zones.Add(zone);
        context.Tariffs.Add(tariff);
        context.Promotions.Add(promo);
        await context.SaveChangesAsync();

        var service = new SessionService(context);

        // Act
        var session = await service.StartSessionAsync(1, 1, 1);

        // Assert
        session.FinalPrice.Should().Be(50);
    }

    [Fact]
    public async Task FixedDiscount30_FinalPrice_IsPriceMinus30()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var client = new Client { Id = 1, FullName = "Test", CardCode = "CARD001" };
        var zone = new Zone { Id = 1, Name = "Test Zone" };
        var tariff = new Tariff { Id = 1, Label = "1 час", Price = 100, Duration = TimeSpan.FromHours(1), ZoneId = 1 };
        var promo = new Promotion { Id = 1, Name = "30 lei скидка", DiscountType = DiscountType.Fixed, DiscountValue = 30, IsActive = true };
        context.Clients.Add(client);
        context.Zones.Add(zone);
        context.Tariffs.Add(tariff);
        context.Promotions.Add(promo);
        await context.SaveChangesAsync();

        var service = new SessionService(context);

        // Act
        var session = await service.StartSessionAsync(1, 1, 1);

        // Assert
        session.FinalPrice.Should().Be(70);
    }

    [Fact]
    public async Task FixedDiscountLargerThanPrice_FinalPrice_IsZero()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var client = new Client { Id = 1, FullName = "Test", CardCode = "CARD001" };
        var zone = new Zone { Id = 1, Name = "Test Zone" };
        var tariff = new Tariff { Id = 1, Label = "1 час", Price = 20, Duration = TimeSpan.FromHours(1), ZoneId = 1 };
        var promo = new Promotion { Id = 1, Name = "50 lei скидка", DiscountType = DiscountType.Fixed, DiscountValue = 50, IsActive = true };
        context.Clients.Add(client);
        context.Zones.Add(zone);
        context.Tariffs.Add(tariff);
        context.Promotions.Add(promo);
        await context.SaveChangesAsync();

        var service = new SessionService(context);

        // Act
        var session = await service.StartSessionAsync(1, 1, 1);

        // Assert
        session.FinalPrice.Should().Be(0);
    }

    [Fact]
    public async Task PercentDiscount100_FinalPrice_IsZero()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var client = new Client { Id = 1, FullName = "Test", CardCode = "CARD001" };
        var zone = new Zone { Id = 1, Name = "Test Zone" };
        var tariff = new Tariff { Id = 1, Label = "1 час", Price = 100, Duration = TimeSpan.FromHours(1), ZoneId = 1 };
        var promo = new Promotion { Id = 1, Name = "100% скидка", DiscountType = DiscountType.Percent, DiscountValue = 100, IsActive = true };
        context.Clients.Add(client);
        context.Zones.Add(zone);
        context.Tariffs.Add(tariff);
        context.Promotions.Add(promo);
        await context.SaveChangesAsync();

        var service = new SessionService(context);

        // Act
        var session = await service.StartSessionAsync(1, 1, 1);

        // Assert
        session.FinalPrice.Should().Be(0);
    }
}