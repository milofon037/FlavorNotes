using FlavorNotes.DTO;

namespace FlavorNotes.Services.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CategoryDto dto, string userRole);
    Task<CategoryDto> UpdateAsync(int id, CategoryDto dto, string userRole);
    Task DeleteAsync(int id, string userRole);
}

