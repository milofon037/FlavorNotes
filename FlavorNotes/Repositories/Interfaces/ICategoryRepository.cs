using FlavorNotes.Models.Entities;
using FlavorNotes.DTO;

namespace FlavorNotes.Repositories.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<PagedResponseDto<CategoryDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<Category?> GetByIdAsync(int id);
    Task<Category> CreateAsync(Category category);
    Task<Category> UpdateAsync(Category category);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

