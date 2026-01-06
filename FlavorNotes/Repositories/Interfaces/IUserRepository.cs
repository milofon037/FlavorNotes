using FlavorNotes.Models.Entities;

namespace FlavorNotes.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<List<User>> GetAllAsync();
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
}

