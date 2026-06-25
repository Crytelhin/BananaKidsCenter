using EntertainmentCenter.Models;

namespace EntertainmentCenter.Services;

public class ZoneApiService
{
    private readonly ApiService _api;

    public ZoneApiService(ApiService api)
    {
        _api = api;
    }

    public async Task<List<Zone>?> GetAllWithTariffsAsync()
    {
        return await _api.GetAsync<List<Zone>>("/api/zones");
    }

    public async Task<List<Zone>?> GetAllAsync(bool includeInactive = false)
    {
        return await _api.GetAsync<List<Zone>>($"/api/zones?all={includeInactive.ToString().ToLower()}");
    }

    public async Task<Zone?> SaveZoneAsync(Zone zone)
    {
        if (zone.Id == 0)
            return await _api.PostAsync<Zone, Zone>("/api/zones", zone);
        else
            return await _api.PutAsync<Zone, Zone>($"/api/zones/{zone.Id}", zone);
    }

    public async Task<bool> DeleteZoneAsync(int id)
    {
        return await _api.DeleteAsync($"/api/zones/{id}");
    }

    public async Task<Tariff?> SaveTariffAsync(int zoneId, Tariff tariff)
    {
        return await _api.PostAsync<Tariff, Tariff>($"/api/zones/{zoneId}/tariffs", tariff);
    }

    public async Task<bool> DeleteTariffAsync(int id)
    {
        return await _api.DeleteAsync($"/api/tariffs/{id}");
    }
}
