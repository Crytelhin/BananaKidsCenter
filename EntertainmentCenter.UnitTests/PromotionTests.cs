using Xunit;
using EntertainmentCenter.API.Models;
using EntertainmentCenter.API.Services;
using FluentAssertions;

namespace EntertainmentCenter.UnitTests;

public class PromotionTests
{
    [Fact]
    public async Task ApplicableDayNull_ValidOnAnyDay()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var promo = new Promotion
        {
            Name = "Акция любой день",
            DiscountType = DiscountType.Percent,
            DiscountValue = 10,
            ApplicableDay = null,
            IsActive = true
        };
        context.Promotions.Add(promo);
        await context.SaveChangesAsync();

        var service = new PromotionService(context);

        // Act
        var promotions = await service.GetActiveAsync();

        // Assert
        promotions.Should().ContainSingle(p => p.Name == "Акция любой день");
        promotions[0].ApplicableDay.Should().BeNull();
    }

    [Fact]
    public async Task ApplicableDayWednesday_ValidOnWednesday()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var promo = new Promotion
        {
            Name = "Акция среда",
            DiscountType = DiscountType.Fixed,
            DiscountValue = 20,
            ApplicableDay = DayOfWeek.Wednesday,
            IsActive = true
        };
        context.Promotions.Add(promo);
        await context.SaveChangesAsync();

        var service = new PromotionService(context);

        // Act
        var promotions = await service.GetActiveAsync();

        // Assert
        var wedPromo = promotions.Should().ContainSingle(p => p.Name == "Акция среда").Subject;
        wedPromo.ApplicableDay.Should().Be(DayOfWeek.Wednesday);
    }

    [Fact]
    public async Task ApplicableDayWednesday_NotMonday()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var promo = new Promotion
        {
            Name = "Акция среды",
            DiscountType = DiscountType.Fixed,
            DiscountValue = 20,
            ApplicableDay = DayOfWeek.Wednesday,
            IsActive = true
        };
        context.Promotions.Add(promo);
        await context.SaveChangesAsync();

        var service = new PromotionService(context);

        // Act
        var promotions = await service.GetActiveAsync();

        // Assert
        var wedPromo = promotions.Should().ContainSingle().Subject;
        wedPromo.ApplicableDay.Should().NotBe(DayOfWeek.Monday);
    }

    [Fact]
    public async Task IsActiveFalse_NotReturnedByGetActive()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Promotions.Add(new Promotion { Name = "Активная", DiscountType = DiscountType.Percent, DiscountValue = 5, IsActive = true });
        context.Promotions.Add(new Promotion { Name = "Неактивная", DiscountType = DiscountType.Percent, DiscountValue = 10, IsActive = false });
        await context.SaveChangesAsync();

        var service = new PromotionService(context);

        // Act
        var promotions = await service.GetActiveAsync();

        // Assert
        promotions.Should().ContainSingle(p => p.Name == "Активная");
        promotions.Should().NotContain(p => p.Name == "Неактивная");
    }
}