using Microsoft.EntityFrameworkCore;
using FlavorNotes.Data;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly ApplicationDbContext _context;

    public ApiKeyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiKey?> GetByKeyAsync(string key)
    {
        return await _context.ApiKeys
            .FirstOrDefaultAsync(ak => ak.Key == key && ak.IsActive && ak.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<ApiKey> CreateAsync(ApiKey apiKey)
    {
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();
        return apiKey;
    }
}

