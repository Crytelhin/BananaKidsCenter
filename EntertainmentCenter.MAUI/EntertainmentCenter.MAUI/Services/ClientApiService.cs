using EntertainmentCenter.Models;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.Services;

public class ClientApiService
{
    private readonly ApiService _api;

    public ClientApiService(ApiService api)
    {
        _api = api;
    }

    public async Task<List<Client>?> SearchAsync(string query)
    {
        var encoded = Uri.EscapeDataString(query);
        return await _api.GetAsync<List<Client>>($"/api/clients/search?q={encoded}");
    }

    public async Task<Client?> GetByCardCodeAsync(string code)
    {
        return await _api.GetAsync<Client>($"/api/clients/card/{code}");
    }

    public async Task<Client?> GetByIdAsync(int id)
    {
        return await _api.GetAsync<Client>($"/api/clients/{id}");
    }

    public async Task<Client?> AddAsync(Client client)
    {
        try
        {
            return await _api.PostAsync<Client, Client>("/api/clients", client);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Duplicate card code
            throw new InvalidOperationException(AppResources.CardAlreadyRegistered);
        }
    }
}
