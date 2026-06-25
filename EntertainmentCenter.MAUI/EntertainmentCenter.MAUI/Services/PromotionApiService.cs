using EntertainmentCenter.Models;

namespace EntertainmentCenter.Services;

public class PromotionApiService
{
    private readonly ApiService _api;

    public PromotionApiService(ApiService api)
    {
        _api = api;
    }

    public async Task<List<Promotion>?> GetActiveAsync()
    {
        return await _api.GetAsync<List<Promotion>>("/api/promotions");
    }

    public async Task<List<Promotion>?> GetAllAsync()
    {
        return await _api.GetAsync<List<Promotion>>("/api/promotions?all=true");
    }

    public async Task<Promotion?> SavePromotionAsync(Promotion promotion)
    {
        if (promotion.Id == 0)
            return await _api.PostAsync<Promotion, Promotion>("/api/promotions", promotion);
        else
            return await _api.PutAsync<Promotion, Promotion>($"/api/promotions/{promotion.Id}", promotion);
    }

    public async Task<bool> DeletePromotionAsync(int id)
    {
        return await _api.DeleteAsync($"/api/promotions/{id}");
    }
}
