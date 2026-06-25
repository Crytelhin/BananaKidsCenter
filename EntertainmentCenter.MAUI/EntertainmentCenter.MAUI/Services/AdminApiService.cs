namespace EntertainmentCenter.Services;

public class AdminApiService
{
    private readonly ApiService _api;

    public AdminApiService(ApiService api)
    {
        _api = api;
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        var result = await _api.PostAsync<VerifyPinRequest, bool>("/api/admin/verify-pin", new VerifyPinRequest { Pin = pin });
        return result;
    }

    public async Task<bool> ChangePinAsync(string newPin)
    {
        var result = await _api.PostAsync<ChangePinRequest, bool>("/api/admin/change-pin", new ChangePinRequest { NewPin = newPin });
        return result;
    }

    public async Task<Models.DashboardMetrics?> GetDashboardAsync()
    {
        return await _api.GetAsync<Models.DashboardMetrics>("/api/admin/dashboard");
    }

    public async Task<Models.NotificationSettings?> GetNotificationSettingsAsync()
    {
        return await _api.GetAsync<Models.NotificationSettings>("/api/admin/notification-settings");
    }

    public async Task<bool> UpdateNotificationSettingsAsync(bool enabled, int minutes)
    {
        return await _api.PutAsync<object>("/api/admin/notification-settings", new
        {
            WarningEnabled = enabled,
            WarningMinutesBeforeExpiry = minutes
        });
    }

    public async Task<byte[]?> GetDailyReportCsvAsync(DateTime date, string lang = "ru", string period = "day")
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        return await _api.GetByteArrayAsync($"/api/reports/daily?date={dateStr}&lang={lang}&period={period}");
    }

    private class VerifyPinRequest
    {
        public string Pin { get; set; } = "";
    }

    private class ChangePinRequest
    {
        public string NewPin { get; set; } = "";
    }
}
