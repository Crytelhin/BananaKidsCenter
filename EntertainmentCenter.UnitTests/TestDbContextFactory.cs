using Microsoft.EntityFrameworkCore;
using EntertainmentCenter.API.Data;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.UnitTests;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        // Seed AppConfig (required by AdminService)
        context.AppConfigs.Add(new AppConfig { Id = 1, AdminPin = "1234" });
        context.SaveChanges();

        return context;
    }
}