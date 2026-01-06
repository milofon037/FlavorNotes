using FlavorNotes.DTO;

namespace FlavorNotes.Services.Interfaces;

public interface IRecipeService
{
    Task<RecipeDto?> GetByIdAsync(int id, string? userRole, int? userId);
    Task<PagedResponseDto<RecipeDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<RecipeDto> CreateAsync(CreateRecipeDto dto, int userId, string userRole);
    Task<RecipeDto> UpdateAsync(int id, UpdateRecipeDto dto, int userId, string userRole);
    Task DeleteAsync(int id, int userId, string userRole);
    Task<List<RecipeDto>> GetByUserAsync(int userId);
    Task<List<RecipeDto>> GetFavoritesByUserAsync(int userId);
    Task AddFavoriteAsync(int recipeId, int userId);
    Task RemoveFavoriteAsync(int recipeId, int userId);
}

