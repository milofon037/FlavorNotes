using FlavorNotes.Models.Entities;
using FlavorNotes.DTO;

namespace FlavorNotes.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<List<User>> GetAllAsync();
    Task<PagedResponseDto<UserDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
}

