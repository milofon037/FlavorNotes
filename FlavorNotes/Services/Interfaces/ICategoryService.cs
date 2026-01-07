using FlavorNotes.DTO;

namespace FlavorNotes.Services.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<PagedResponseDto<CategoryDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CategoryDto dto, string userRole);
    Task<CategoryDto> UpdateAsync(int id, CategoryDto dto, string userRole);
    Task DeleteAsync(int id, string userRole);
}

