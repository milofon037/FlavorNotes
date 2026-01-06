using FlavorNotes.Models.Entities;
using FlavorNotes.DTO;

namespace FlavorNotes.Repositories.Interfaces;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(int id);
    Task<PagedResponseDto<RecipeDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<Recipe> CreateAsync(Recipe recipe);
    Task<Recipe> UpdateAsync(Recipe recipe);
    Task DeleteAsync(int id);
    Task SoftDeleteAsync(int id);
    Task HardDeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<List<Recipe>> GetByUserAsync(int userId);
    Task<List<Recipe>> GetFavoritesByUserAsync(int userId);
    Task AddFavoriteAsync(int recipeId, int userId);
    Task RemoveFavoriteAsync(int recipeId, int userId);
}

