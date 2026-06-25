using Microsoft.EntityFrameworkCore;
using EntertainmentCenter.API.Data;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.API.Services
{
    public class PromotionService
    {
        private readonly AppDbContext _context;

        public PromotionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Promotion>> GetActiveAsync()
        {
            return await _context.Promotions
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        public async Task<List<Promotion>> GetAllAsync()
        {
            return await _context.Promotions.ToListAsync();
        }

        public async Task<Promotion?> GetByIdAsync(int id)
        {
            return await _context.Promotions.FindAsync(id);
        }

        public async Task<Promotion> SavePromotionAsync(Promotion promotion)
        {
            if (promotion.Id == 0)
            {
                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();
                return promotion;
            }

            var existing = await _context.Promotions.FindAsync(promotion.Id);
            if (existing == null)
            {
                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();
                return promotion;
            }

            // Update properties — avoid tracking conflict with _context.Update()
            existing.Name = (promotion.Name ?? "").Trim();
            existing.DiscountType = promotion.DiscountType;
            existing.DiscountValue = promotion.DiscountValue;
            existing.ApplicableDay = promotion.ApplicableDay;
            existing.IsActive = promotion.IsActive;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeletePromotionAsync(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo != null)
            {
                promo.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}