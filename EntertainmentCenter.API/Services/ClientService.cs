using Microsoft.EntityFrameworkCore;
using EntertainmentCenter.API.Data;
using EntertainmentCenter.API.Models;

namespace EntertainmentCenter.API.Services
{
    public class ClientService
    {
        private readonly AppDbContext _context;

        public ClientService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Client>> SearchAsync(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await _context.Clients.ToListAsync();
            }
            var lowerQuery = query.ToLower();
            return await _context.Clients
                .Where(c => c.FullName.ToLower().Contains(lowerQuery) ||
                            c.CardCode.ToLower().Contains(lowerQuery))
                .ToListAsync();
        }

        public async Task<Client?> GetByIdAsync(int id)
        {
            return await _context.Clients.FindAsync(id);
        }

        public async Task<Client?> GetByCardCodeAsync(string code)
        {
            return await _context.Clients
                .FirstOrDefaultAsync(c => c.CardCode == code);
        }

        public async Task<Client> AddAsync(Client client)
        {
            // CardCode must be unique
            var existing = await _context.Clients
                .FirstOrDefaultAsync(c => c.CardCode == client.CardCode);
            if (existing != null)
                throw new InvalidOperationException("Карта уже зарегистрирована на другого клиента");

            client.RegisteredAt = DateTime.UtcNow;
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            return client;
        }
    }
}