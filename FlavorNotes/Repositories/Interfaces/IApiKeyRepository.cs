using FlavorNotes.Models.Entities;

namespace FlavorNotes.Repositories.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByKeyAsync(string key);
    Task<ApiKey> CreateAsync(ApiKey apiKey);
}

