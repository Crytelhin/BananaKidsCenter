using Microsoft.EntityFrameworkCore;
using EntertainmentCenter.API.Data;
using EntertainmentCenter.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

if (builder.Environment.IsEnvironment("IntegrationTest"))
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseInMemoryDatabase("IntegrationTests"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
}

// Register as Windows Service
builder.Host.UseWindowsService();

// Register services
builder.Services.AddScoped<ZoneService>();
builder.Services.AddScoped<TariffService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddHostedService<UdpDiscoveryHostedService>();

var app = builder.Build();

// Auto-create database tables on first run (works for both new DB and existing)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    // Add new columns that EnsureCreated won't add to existing tables
    try
    {
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE \"AppConfigs\" ADD COLUMN IF NOT EXISTS \"WarningEnabled\" boolean NOT NULL DEFAULT true");
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE \"AppConfigs\" ADD COLUMN IF NOT EXISTS \"WarningMinutesBeforeExpiry\" integer NOT NULL DEFAULT 5");

        // Session activation columns (added for first-scan activation feature)
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE \"Sessions\" ADD COLUMN IF NOT EXISTS \"CreatedAt\" timestamp with time zone NOT NULL DEFAULT now()");
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE \"Sessions\" ADD COLUMN IF NOT EXISTS \"ActivatedAt\" timestamp with time zone NULL");
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE \"Sessions\" ADD COLUMN IF NOT EXISTS \"DurationMinutes\" integer NOT NULL DEFAULT 60");
        // Migrate existing EntryTime data to CreatedAt if EntryTime column still exists
        context.Database.ExecuteSqlRaw(
            "UPDATE \"Sessions\" SET \"CreatedAt\" = \"EntryTime\" WHERE \"CreatedAt\" = \"EntryTime\" AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Sessions' AND column_name='EntryTime')");
        // Drop old EntryTime column to avoid NOT NULL constraint violations on new inserts
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE \"Sessions\" DROP COLUMN IF EXISTS \"EntryTime\"");
        // Mark existing active sessions as activated (they were already running)
        context.Database.ExecuteSqlRaw(
            "UPDATE \"Sessions\" SET \"ActivatedAt\" = \"CreatedAt\", \"IsActive\" = true WHERE \"ActivatedAt\" IS NULL AND \"IsActive\" = true");
    }
    catch { /* ignore — columns may already exist */ }
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
