using FlavorNotes.DTO;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FlavorNotes.Services;

public class IngredientService : IIngredientService
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<IngredientService> _logger;

    public IngredientService(
        IIngredientRepository ingredientRepository,
        IDistributedCache cache,
        ILogger<IngredientService> logger)
    {
        _ingredientRepository = ingredientRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<IngredientDto>> GetAllAsync()
    {
        var cacheKey = "ingredients:all";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<List<IngredientDto>>(cached) ?? new();
        }

        var ingredients = await _ingredientRepository.GetAllAsync();
        var dtos = ingredients.Select(i => new IngredientDto
        {
            IngredientId = i.IngredientId,
            Name = i.Name
        }).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return dtos;
    }

    public async Task<PagedResponseDto<IngredientDto>> GetPagedAsync(int page, int pageSize, string? search)
    {
        var cacheKey = $"ingredients:paged:{page}:{pageSize}:{search ?? ""}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<PagedResponseDto<IngredientDto>>(cached)!;
        }

        var result = await _ingredientRepository.GetPagedAsync(page, pageSize, search);
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return result;
    }

    public async Task<IngredientDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"ingredient:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<IngredientDto>(cached);
        }

        var ingredient = await _ingredientRepository.GetByIdAsync(id);
        if (ingredient == null)
        {
            return null;
        }

        var dto = new IngredientDto
        {
            IngredientId = ingredient.IngredientId,
            Name = ingredient.Name
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return dto;
    }

    public async Task<IngredientDto> CreateAsync(IngredientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Ingredient name is required");
        }

        var ingredient = new Ingredient
        {
            Name = dto.Name
        };

        var created = await _ingredientRepository.CreateAsync(ingredient);
        _logger.LogInformation("Ingredient {IngredientId} created: {IngredientName}", created.IngredientId, created.Name);

        await InvalidateCacheAsync();

        return new IngredientDto
        {
            IngredientId = created.IngredientId,
            Name = created.Name
        };
    }

    private async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync("ingredients:all");
    }
}

