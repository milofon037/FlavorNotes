using FlavorNotes.Models.Entities;
using FlavorNotes.DTO;

namespace FlavorNotes.Repositories.Interfaces;

public interface IIngredientRepository
{
    Task<List<Ingredient>> GetAllAsync();
    Task<PagedResponseDto<IngredientDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<Ingredient?> GetByIdAsync(int id);
    Task<Ingredient> CreateAsync(Ingredient ingredient);
    Task<bool> ExistsAsync(int id);
}


