using FlavorNotes.DTO;

namespace FlavorNotes.Services.Interfaces;

public interface IIngredientService
{
    Task<List<IngredientDto>> GetAllAsync();
    Task<PagedResponseDto<IngredientDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<IngredientDto?> GetByIdAsync(int id);
    Task<IngredientDto> CreateAsync(IngredientDto dto);
}

