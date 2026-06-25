using Microsoft.EntityFrameworkCore;
using EntertainmentCenter.API.Data;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.API.Services
{
    public class SessionService
    {
        private readonly AppDbContext _context;

        public SessionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Session>> GetAllActiveAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = DateTime.Today.ToUniversalTime();
            return await _context.Sessions
                .Where(s =>
                    (s.IsActive && s.ActivatedAt != null && s.ExpiresAt > now)
                    || (s.ActivatedAt == null && !s.IsActive && s.CreatedAt >= todayStart)
                )
                .Include(s => s.Client)
                .Include(s => s.Tariff)
                    .ThenInclude(t => t.Zone)
                .Include(s => s.Promotion)
                .ToListAsync();
        }

        public async Task<Session?> GetActiveByCardCodeAsync(string cardCode)
        {
            var now = DateTime.UtcNow;
            return await _context.Sessions
                .Include(s => s.Client)
                .Include(s => s.Tariff)
                    .ThenInclude(t => t.Zone)
                .Include(s => s.Promotion)
                .FirstOrDefaultAsync(s => s.Client != null
                    && s.Client.CardCode == cardCode
                    && s.IsActive
                    && s.ActivatedAt != null
                    && s.ExpiresAt > now);
        }

        public async Task<Session?> GetByIdAsync(int id)
        {
            return await _context.Sessions
                .Include(s => s.Client)
                .Include(s => s.Tariff)
                    .ThenInclude(t => t.Zone)
                .Include(s => s.Promotion)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Создаёт сессию в статусе "ожидает входа" (IsActive = false) или активирует сразу.
        /// </summary>
        public async Task<Session> StartSessionAsync(int clientId, int tariffId, int? promotionId, int? customDurationMinutes = null, bool activateImmediately = false)
        {
            var client = await _context.Clients.FindAsync(clientId);
            var tariff = await _context.Tariffs.FindAsync(tariffId);

            if (client == null || tariff == null)
                throw new ArgumentException("Client or Tariff not found");

            var duration = customDurationMinutes.HasValue && customDurationMinutes.Value > 0
                ? TimeSpan.FromMinutes(customDurationMinutes.Value)
                : tariff.Duration;

            decimal price = tariff.Price;
            if (customDurationMinutes.HasValue && customDurationMinutes.Value > 0 && tariff.Duration.TotalMinutes > 0)
            {
                var ratio = (decimal)(customDurationMinutes.Value / tariff.Duration.TotalMinutes);
                price = tariff.Price * ratio;
            }

            if (promotionId.HasValue)
            {
                var promo = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.Id == promotionId.Value);

                if (promo != null)
                {
                    if (promo.DiscountType == DiscountType.Percent)
                        price -= price * (promo.DiscountValue / 100);
                    else
                        price -= promo.DiscountValue;
                }
            }

            var session = new Session
            {
                ClientId = clientId,
                TariffId = tariffId,
                Client = client,
                Tariff = tariff,
                PromotionId = promotionId,
                FinalPrice = Math.Round(Math.Max(0, price), 2),
                DurationMinutes = (int)duration.TotalMinutes,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = activateImmediately ? DateTime.UtcNow : null,                        // если без карты — активируем сразу
                ExpiresAt = activateImmediately ? DateTime.UtcNow.Add(duration) : DateTime.UtcNow.AddYears(10),
                IsActive = activateImmediately                           // если без карты — активна сразу
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        /// <summary>
        /// Активирует сессию при первом сканировании карточки на контроле.
        /// Запускает таймер: ExpiresAt = now + DurationMinutes.
        /// Если сессия уже активна — возвращает null (не перезапускает таймер).
        /// </summary>
        public async Task<Session?> ActivateSessionAsync(string cardCode)
        {
            var session = await _context.Sessions
                .Include(s => s.Client)
                .Include(s => s.Tariff).ThenInclude(t => t.Zone)
                .Include(s => s.Promotion)
                .FirstOrDefaultAsync(s =>
                    s.Client != null &&
                    s.Client.CardCode == cardCode &&
                    s.ActivatedAt == null &&   // ещё не активирована
                    !s.IsActive);

            if (session == null) return null;

            session.ActivatedAt = DateTime.UtcNow;
            session.ExpiresAt = session.ActivatedAt.Value.AddMinutes(session.DurationMinutes);
            session.IsActive = true;
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<List<Session>> GetHistoryAsync(DateTime from, DateTime to)
        {
            return await _context.Sessions
                .Where(s => s.CreatedAt >= from && s.CreatedAt <= to)
                .Include(s => s.Client)
                .Include(s => s.Tariff)
                    .ThenInclude(t => t.Zone)
                .Include(s => s.Promotion)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> EndSessionAsync(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return false;

            session.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Session?> ExtendSessionAsync(int id, int tariffId, int? promotionId, int? customDurationMinutes = null)
        {
            var session = await _context.Sessions
                .Include(s => s.Client)
                .Include(s => s.Tariff).ThenInclude(t => t.Zone)
                .Include(s => s.Promotion)
                .FirstOrDefaultAsync(s => s.Id == id);

            var tariff = await _context.Tariffs.FindAsync(tariffId);

            if (session == null || tariff == null)
                return null;

            var duration = customDurationMinutes.HasValue && customDurationMinutes.Value > 0
                ? TimeSpan.FromMinutes(customDurationMinutes.Value)
                : tariff.Duration;

            decimal price = tariff.Price;
            if (customDurationMinutes.HasValue && customDurationMinutes.Value > 0 && tariff.Duration.TotalMinutes > 0)
            {
                var ratio = (decimal)customDurationMinutes.Value / (decimal)tariff.Duration.TotalMinutes;
                price = tariff.Price * ratio;
            }

            if (promotionId.HasValue)
            {
                var promo = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.Id == promotionId.Value);

                if (promo != null)
                {
                    if (promo.DiscountType == DiscountType.Percent)
                        price -= price * (promo.DiscountValue / 100);
                    else
                        price -= promo.DiscountValue;
                }
            }

            session.DurationMinutes += (int)duration.TotalMinutes;
            session.FinalPrice += Math.Round(Math.Max(0, price), 2);

            if (session.ActivatedAt != null)
            {
                var baseTime = session.ExpiresAt > DateTime.UtcNow ? session.ExpiresAt : DateTime.UtcNow;
                session.ExpiresAt = baseTime.Add(duration);
                session.IsActive = true;
            }

            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<DashboardMetrics> GetDashboardMetricsAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);

            var visitsToday = await _context.Sessions
                .CountAsync(s => s.CreatedAt >= todayStart && s.CreatedAt < todayEnd);

            var activeNow = await _context.Sessions
                .CountAsync(s => s.IsActive && s.ExpiresAt > now);

            var revenueToday = await _context.Sessions
                .Where(s => s.CreatedAt >= todayStart && s.CreatedAt < todayEnd)
                .SumAsync(s => s.FinalPrice);

            return new DashboardMetrics
            {
                VisitsToday = visitsToday,
                ActiveNow = activeNow,
                RevenueToday = revenueToday
            };
        }
    }

    public class DashboardMetrics
    {
        public int VisitsToday { get; set; }
        public int ActiveNow { get; set; }
        public decimal RevenueToday { get; set; }
    }
}
