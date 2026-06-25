using System.Net.Http.Json;
using System.Text.Json;

namespace EntertainmentCenter.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(ServerAddressManager addressManager, DiscoveryHttpMessageHandler handler)
    {
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(addressManager.CurrentBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _http.GetAsync(endpoint);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<byte[]?> GetByteArrayAsync(string endpoint)
    {
        var response = await _http.GetAsync(endpoint);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var response = await _http.PostAsJsonAsync(endpoint, data, _jsonOptions);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        var response = await _http.DeleteAsync(endpoint);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
    {
        var response = await _http.PostAsJsonAsync(endpoint, data, _jsonOptions);
        return response.IsSuccessStatusCode;
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var response = await _http.PutAsJsonAsync(endpoint, data, _jsonOptions);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }

    public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest data)
    {
        var response = await _http.PutAsJsonAsync(endpoint, data, _jsonOptions);
        return response.IsSuccessStatusCode;
    }
}
