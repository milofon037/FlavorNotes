using FlavorNotes.DTO;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FlavorNotes.Services;

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RecipeService> _logger;

    public RecipeService(
        IRecipeRepository recipeRepository,
        ICategoryRepository categoryRepository,
        IDistributedCache cache,
        ILogger<RecipeService> logger)
    {
        _recipeRepository = recipeRepository;
        _categoryRepository = categoryRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RecipeDto?> GetByIdAsync(int id, string? userRole, int? userId)
    {
        var cacheKey = $"recipe:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<RecipeDto>(cached);
        }

        var recipe = await _recipeRepository.GetByIdAsync(id);
        if (recipe == null)
        {
            return null;
        }

        var dto = MapToDto(recipe);
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return dto;
    }

    public async Task<PagedResponseDto<RecipeDto>> GetPagedAsync(int page, int pageSize, string? search)
    {
        var cacheKey = $"recipes:paged:{page}:{pageSize}:{search ?? ""}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<PagedResponseDto<RecipeDto>>(cached)!;
        }

        var result = await _recipeRepository.GetPagedAsync(page, pageSize, search);
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return result;
    }

    public async Task<RecipeDto> CreateAsync(CreateRecipeDto dto, int userId, string userRole)
    {
        if (userRole != "Admin" && userRole != "Manager" && userRole != "User")
        {
            throw new UnauthorizedAccessException("You don't have permission to create recipes");
        }

        if (!await _categoryRepository.ExistsAsync(dto.CategoryId))
        {
            throw new KeyNotFoundException("Category not found");
        }

        var recipe = new Recipe
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            Title = dto.Title,
            Description = dto.Description,
            PrepTimeMinutes = dto.PrepTimeMinutes,
            CookTimeMinutes = dto.CookTimeMinutes,
            Servings = dto.Servings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RecipeIngredients = dto.Ingredients.Select(i => new RecipeIngredient
            {
                IngredientId = i.IngredientId,
                UnitId = i.UnitId,
                Quantity = i.Quantity
            }).ToList(),
            RecipeTags = dto.TagIds.Select(tagId => new RecipeTag
            {
                TagId = tagId
            }).ToList(),
            InstructionSteps = dto.Instructions.Select(i => new InstructionStep
            {
                StepNumber = i.StepNumber,
                InstructionText = i.InstructionText
            }).ToList()
        };

        var created = await _recipeRepository.CreateAsync(recipe);
        _logger.LogInformation("Recipe {RecipeId} created by user {UserId}", created.RecipeId, userId);
        
        await InvalidateCacheAsync();
        
        var fullRecipe = await _recipeRepository.GetByIdAsync(created.RecipeId);
        return MapToDto(fullRecipe!);
    }

    public async Task<RecipeDto> UpdateAsync(int id, UpdateRecipeDto dto, int userId, string userRole)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id);
        if (recipe == null)
        {
            throw new KeyNotFoundException("Recipe not found");
        }

        if (userRole == "User" && recipe.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only update your own recipes");
        }
        
        if (userRole != "Admin" && userRole != "Manager" && userRole != "User")
        {
            throw new UnauthorizedAccessException("You don't have permission to update recipes");
        }

        recipe.CategoryId = dto.CategoryId;
        recipe.Title = dto.Title;
        recipe.Description = dto.Description;
        recipe.PrepTimeMinutes = dto.PrepTimeMinutes;
        recipe.CookTimeMinutes = dto.CookTimeMinutes;
        recipe.Servings = dto.Servings;
        recipe.UpdatedAt = DateTime.UtcNow;

        recipe.RecipeIngredients.Clear();
        recipe.RecipeTags.Clear();
        recipe.InstructionSteps.Clear();

        foreach (var ingredient in dto.Ingredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                IngredientId = ingredient.IngredientId,
                UnitId = ingredient.UnitId,
                Quantity = ingredient.Quantity
            });
        }

        foreach (var tagId in dto.TagIds)
        {
            recipe.RecipeTags.Add(new RecipeTag
            {
                TagId = tagId
            });
        }

        foreach (var instruction in dto.Instructions)
        {
            recipe.InstructionSteps.Add(new InstructionStep
            {
                StepNumber = instruction.StepNumber,
                InstructionText = instruction.InstructionText
            });
        }

        await _recipeRepository.UpdateAsync(recipe);
        _logger.LogInformation("Recipe {RecipeId} updated by user {UserId}", id, userId);
        
        await InvalidateCacheAsync();
        
        var updated = await _recipeRepository.GetByIdAsync(id);
        return MapToDto(updated!);
    }

    public async Task DeleteAsync(int id, int userId, string userRole)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id);
        if (recipe == null)
        {
            throw new KeyNotFoundException("Recipe not found");
        }

        if (userRole != "Admin")
        {
            throw new UnauthorizedAccessException("Only Admin can delete recipes");
        }
        
        await _recipeRepository.HardDeleteAsync(id);
        _logger.LogInformation("Recipe {RecipeId} deleted by admin user {UserId}", id, userId);

        await InvalidateCacheAsync();
    }

    public async Task<List<RecipeDto>> GetByUserAsync(int userId)
    {
        var recipes = await _recipeRepository.GetByUserAsync(userId);
        return recipes.Select(MapToDto).ToList();
    }

    public async Task<List<RecipeDto>> GetFavoritesByUserAsync(int userId)
    {
        var favorites = await _recipeRepository.GetFavoritesByUserAsync(userId);
        return favorites.Select(MapToDto).ToList();
    }

    public async Task AddFavoriteAsync(int recipeId, int userId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            throw new KeyNotFoundException("Recipe not found");
        }

        await _recipeRepository.AddFavoriteAsync(recipeId, userId);
        _logger.LogInformation("User {UserId} added recipe {RecipeId} to favorites", userId, recipeId);
    }

    public async Task RemoveFavoriteAsync(int recipeId, int userId)
    {
        await _recipeRepository.RemoveFavoriteAsync(recipeId, userId);
        _logger.LogInformation("User {UserId} removed recipe {RecipeId} from favorites", userId, recipeId);
    }

    private RecipeDto MapToDto(Recipe recipe)
    {
        return new RecipeDto
        {
            RecipeId = recipe.RecipeId,
            UserId = recipe.UserId,
            Username = recipe.User.Username,
            CategoryId = recipe.CategoryId,
            CategoryName = recipe.Category.Name,
            Title = recipe.Title,
            Description = recipe.Description,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookTimeMinutes = recipe.CookTimeMinutes,
            Servings = recipe.Servings,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt,
            Ingredients = recipe.RecipeIngredients.Select(ri => new RecipeIngredientDto
            {
                IngredientId = ri.IngredientId,
                IngredientName = ri.Ingredient.Name,
                UnitId = ri.UnitId,
                UnitName = ri.Unit.Name,
                UnitAbbreviation = ri.Unit.Abbreviation,
                Quantity = ri.Quantity
            }).ToList(),
            Tags = recipe.RecipeTags.Select(rt => rt.Tag.Name).ToList(),
            Instructions = recipe.InstructionSteps
                .OrderBy(step => step.StepNumber)
                .Select(step => new InstructionStepDto
                {
                    StepNumber = step.StepNumber,
                    InstructionText = step.InstructionText
                })
                .ToList()
        };
    }

    private async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync("recipe:*");
        await _cache.RemoveAsync("recipes:paged:*");
    }
}

