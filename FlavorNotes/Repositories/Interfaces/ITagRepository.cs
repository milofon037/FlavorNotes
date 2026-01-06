using FlavorNotes.Models.Entities;

namespace FlavorNotes.Repositories.Interfaces;

public interface ITagRepository
{
    Task<List<Tag>> GetAllAsync();
    Task<Tag?> GetByIdAsync(int id);
    Task<Tag> CreateAsync(Tag tag);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

