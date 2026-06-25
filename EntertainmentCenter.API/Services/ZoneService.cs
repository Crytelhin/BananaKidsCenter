using Microsoft.EntityFrameworkCore;
using EntertainmentCenter.API.Data;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.API.Services
{
    public class ZoneService
    {
        private readonly AppDbContext _context;

        public ZoneService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Zone>> GetAllWithTariffsAsync(bool includeInactive = false)
        {
            var query = _context.Zones.AsQueryable();
            if (!includeInactive)
                query = query.Where(z => z.IsActive);

            return await query
                .Include(z => z.Tariffs.Where(t => includeInactive || t.IsActive))
                .ToListAsync();
        }

        public async Task<Zone?> GetByIdAsync(int id)
        {
            return await _context.Zones
                .Include(z => z.Tariffs.Where(t => t.IsActive))
                .FirstOrDefaultAsync(z => z.Id == id);
        }

        public async Task<Zone> SaveZoneAsync(Zone zone)
        {
            if (zone.Id == 0)
            {
                _context.Zones.Add(zone);
                await _context.SaveChangesAsync();
                return zone;
            }

            var existing = await _context.Zones.FindAsync(zone.Id);
            if (existing == null)
            {
                _context.Zones.Add(zone);
                await _context.SaveChangesAsync();
                return zone;
            }

            // Update properties — avoid tracking conflict with _context.Update()
            existing.Name = zone.Name?.Trim() ?? "";
            existing.IsActive = zone.IsActive;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteZoneAsync(int id)
        {
            var zone = await _context.Zones
                .Include(z => z.Tariffs)
                .FirstOrDefaultAsync(z => z.Id == id);

            if (zone == null) return;

            // End all active sessions using any of this zone's tariffs
            var tariffIds = zone.Tariffs.Select(t => t.Id).ToList();
            if (tariffIds.Count > 0)
            {
                var activeSessions = await _context.Sessions
                    .Where(s => tariffIds.Contains(s.TariffId) && s.IsActive)
                    .ToListAsync();

                foreach (var session in activeSessions)
                    session.IsActive = false;
            }

            // Hard delete zone (cascade deletes tariffs)
            _context.Zones.Remove(zone);
            await _context.SaveChangesAsync();
        }
    }
}
