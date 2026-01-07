using FlavorNotes.DTO;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FlavorNotes.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IDistributedCache cache,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var cacheKey = "categories:all";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<List<CategoryDto>>(cached) ?? new();
        }

        var categories = await _categoryRepository.GetAllAsync();
        var dtos = categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            Name = c.Name
        }).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return dtos;
    }

    public async Task<PagedResponseDto<CategoryDto>> GetPagedAsync(int page, int pageSize, string? search)
    {
        var cacheKey = $"categories:paged:{page}:{pageSize}:{search ?? ""}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<PagedResponseDto<CategoryDto>>(cached)!;
        }

        var result = await _categoryRepository.GetPagedAsync(page, pageSize, search);
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return result;
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"category:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<CategoryDto>(cached);
        }

        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            return null;
        }

        var dto = new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return dto;
    }

    public async Task<CategoryDto> CreateAsync(CategoryDto dto, string userRole)
    {
        if (userRole != "Admin" && userRole != "Manager")
        {
            throw new UnauthorizedAccessException("You don't have permission to create categories");
        }

        var category = new Category
        {
            Name = dto.Name
        };

        var created = await _categoryRepository.CreateAsync(category);
        _logger.LogInformation("Category {CategoryId} created", created.CategoryId);

        await InvalidateCacheAsync();

        return new CategoryDto
        {
            CategoryId = created.CategoryId,
            Name = created.Name
        };
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryDto dto, string userRole)
    {
        if (userRole != "Admin")
        {
            throw new UnauthorizedAccessException("You don't have permission to update categories");
        }

        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new KeyNotFoundException("Category not found");
        }

        category.Name = dto.Name;
        var updated = await _categoryRepository.UpdateAsync(category);
        _logger.LogInformation("Category {CategoryId} updated", id);

        await InvalidateCacheAsync();

        return new CategoryDto
        {
            CategoryId = updated.CategoryId,
            Name = updated.Name
        };
    }

    public async Task DeleteAsync(int id, string userRole)
    {
        if (userRole != "Admin")
        {
            throw new UnauthorizedAccessException("You don't have permission to delete categories");
        }

        await _categoryRepository.DeleteAsync(id);
        _logger.LogInformation("Category {CategoryId} deleted", id);

        await InvalidateCacheAsync();
    }

    private async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync("categories:all");
    }
}

