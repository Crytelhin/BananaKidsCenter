using Microsoft.EntityFrameworkCore;
using EntertainmentCenter.API.Data;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.API.Services
{
    public class AdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> VerifyPinAsync(string pin)
        {
            var config = await _context.AppConfigs.FindAsync(1);
            return config?.AdminPin == pin;
        }

        public async Task<bool> ChangePinAsync(string newPin)
        {
            var config = await _context.AppConfigs.FindAsync(1);
            if (config == null) return false;
            config.AdminPin = newPin;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AppConfig> GetConfigAsync()
        {
            var config = await _context.AppConfigs.FindAsync(1);
            if (config == null)
            {
                config = new AppConfig { Id = 1, AdminPin = "1234" };
                _context.AppConfigs.Add(config);
                await _context.SaveChangesAsync();
            }
            return config;
        }

        public async Task<bool> UpdateNotificationSettingsAsync(bool enabled, int minutes)
        {
            var config = await _context.AppConfigs.FindAsync(1);
            if (config == null) return false;
            config.WarningEnabled = enabled;
            config.WarningMinutesBeforeExpiry = Math.Max(1, minutes);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}