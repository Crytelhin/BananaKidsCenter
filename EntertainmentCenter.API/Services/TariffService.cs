using EntertainmentCenter.API.Data;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.API.Services
{
    public class TariffService
    {
        private readonly AppDbContext _context;

        public TariffService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Tariff> SaveTariffAsync(Tariff tariff)
        {
            if (tariff.Id == 0)
            {
                _context.Tariffs.Add(tariff);
                await _context.SaveChangesAsync();
                return tariff;
            }

            var existing = await _context.Tariffs.FindAsync(tariff.Id);
            if (existing == null)
            {
                _context.Tariffs.Add(tariff);
                await _context.SaveChangesAsync();
                return tariff;
            }

            // Update properties — avoid tracking conflict with already-loaded entities
            existing.Label = (tariff.Label ?? "").Trim();
            existing.Duration = tariff.Duration;
            existing.Price = tariff.Price;
            existing.IsActive = tariff.IsActive;
            existing.ZoneId = tariff.ZoneId;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteTariffAsync(int id)
        {
            var tariff = await _context.Tariffs.FindAsync(id);
            if (tariff != null)
            {
                tariff.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}
