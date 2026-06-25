using EntertainmentCenter.Models;

namespace EntertainmentCenter.Services;

public class SessionApiService
{
    private readonly ApiService _api;

    public SessionApiService(ApiService api)
    {
        _api = api;
    }

    public async Task<List<Session>?> GetAllActiveAsync()
    {
        return await _api.GetAsync<List<Session>>("/api/sessions/active");
    }

    public async Task<Session?> CheckEntryAsync(string cardCode)
    {
        return await _api.GetAsync<Session>($"/api/sessions/check/{cardCode}");
    }

    public async Task<Session?> StartSessionAsync(int clientId, int tariffId, int? promotionId, int? customDurationMinutes = null, bool activateImmediately = false)
    {
        var request = new StartSessionRequest
        {
            ClientId = clientId,
            TariffId = tariffId,
            PromotionId = promotionId,
            CustomDurationMinutes = customDurationMinutes,
            ActivateImmediately = activateImmediately
        };

        return await _api.PostAsync<StartSessionRequest, Session>("/api/sessions", request);
    }

    public async Task<Session?> GetByIdAsync(int id)
    {
        return await _api.GetAsync<Session>($"/api/sessions/{id}");
    }

    public async Task<List<Session>?> GetHistoryAsync(DateTime from, DateTime to)
    {
        return await _api.GetAsync<List<Session>>(
            $"/api/sessions/history?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}");
    }

    public async Task<bool> EndSessionAsync(int id)
    {
        return await _api.PostAsync<object>($"/api/sessions/{id}/end", new { });
    }

    public async Task<Session?> ExtendSessionAsync(int sessionId, int tariffId, int? promotionId, int? customDurationMinutes = null)
    {
        var request = new ExtendSessionRequest
        {
            TariffId = tariffId,
            PromotionId = promotionId,
            CustomDurationMinutes = customDurationMinutes
        };

        return await _api.PostAsync<ExtendSessionRequest, Session>($"/api/sessions/{sessionId}/extend", request);
    }

    private class StartSessionRequest
    {
        public int ClientId { get; set; }
        public int TariffId { get; set; }
        public int? PromotionId { get; set; }
        public int? CustomDurationMinutes { get; set; }
        public bool ActivateImmediately { get; set; }
    }

    private class ExtendSessionRequest
    {
        public int TariffId { get; set; }
        public int? PromotionId { get; set; }
        public int? CustomDurationMinutes { get; set; }
    }
}
