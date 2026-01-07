using FlavorNotes.Models.Entities;
using FlavorNotes.DTO;

namespace FlavorNotes.Repositories.Interfaces;

public interface ITagRepository
{
    Task<List<Tag>> GetAllAsync();
    Task<PagedResponseDto<TagDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<Tag?> GetByIdAsync(int id);
    Task<Tag> CreateAsync(Tag tag);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}


