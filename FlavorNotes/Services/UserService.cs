using FlavorNotes.DTO;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FlavorNotes.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IDistributedCache cache,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResponseDto<UserDto>> GetPagedAsync(int page, int pageSize, string? search)
    {
        var cacheKey = $"users:paged:{page}:{pageSize}:{search ?? ""}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<PagedResponseDto<UserDto>>(cached)!;
        }

        var result = await _userRepository.GetPagedAsync(page, pageSize, search);
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return result;
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"user:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<UserDto>(cached);
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        var dto = new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return dto;
    }

    public async Task<UserDto> UpdateRoleAsync(int id, UpdateRoleDto dto, string adminRole)
    {
        if (adminRole != "Admin")
        {
            throw new UnauthorizedAccessException("Only Admin can update user roles");
        }

        var allowedRoles = new[] { "Admin", "Manager", "User" };
        if (!allowedRoles.Contains(dto.Role))
        {
            throw new ArgumentException("Invalid role. Allowed roles: Admin, Manager, User");
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        var oldRole = user.Role;
        user.Role = dto.Role;
        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Admin updated role for user {UserId} from {OldRole} to {NewRole}", 
            id, oldRole, dto.Role);

        await InvalidateUserCacheAsync(id);

        return new UserDto
        {
            UserId = updated.UserId,
            Username = updated.Username,
            Email = updated.Email,
            Role = updated.Role,
            CreatedAt = updated.CreatedAt
        };
    }

    private async Task InvalidateUserCacheAsync(int userId)
    {
        await _cache.RemoveAsync($"user:{userId}");
    }
}

