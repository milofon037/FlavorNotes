using FlavorNotes.DTO;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FlavorNotes.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TagService> _logger;

    public TagService(
        ITagRepository tagRepository,
        IDistributedCache cache,
        ILogger<TagService> logger)
    {
        _tagRepository = tagRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<TagDto>> GetAllAsync()
    {
        var cacheKey = "tags:all";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<List<TagDto>>(cached) ?? new();
        }

        var tags = await _tagRepository.GetAllAsync();
        var dtos = tags.Select(t => new TagDto
        {
            TagId = t.TagId,
            Name = t.Name
        }).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return dtos;
    }

    public async Task<PagedResponseDto<TagDto>> GetPagedAsync(int page, int pageSize, string? search)
    {
        var cacheKey = $"tags:paged:{page}:{pageSize}:{search ?? ""}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<PagedResponseDto<TagDto>>(cached)!;
        }

        var result = await _tagRepository.GetPagedAsync(page, pageSize, search);
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return result;
    }

    public async Task<TagDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"tag:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<TagDto>(cached);
        }

        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null)
        {
            return null;
        }

        var dto = new TagDto
        {
            TagId = tag.TagId,
            Name = tag.Name
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return dto;
    }

    public async Task<TagDto> CreateAsync(TagDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Tag name is required");
        }

        var tag = new Tag
        {
            Name = dto.Name
        };

        var created = await _tagRepository.CreateAsync(tag);
        _logger.LogInformation("Tag {TagId} created: {TagName}", created.TagId, created.Name);

        await InvalidateCacheAsync();

        return new TagDto
        {
            TagId = created.TagId,
            Name = created.Name
        };
    }

    public async Task DeleteAsync(int id, string userRole)
    {
        if (userRole != "Admin")
        {
            throw new UnauthorizedAccessException("Only Admin can delete tags");
        }

        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null)
        {
            throw new KeyNotFoundException("Tag not found");
        }

        await _tagRepository.DeleteAsync(id);
        _logger.LogInformation("Tag {TagId} deleted by {Role}", id, userRole);

        await InvalidateCacheAsync();
    }

    private async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync("tags:all");
    }
}

