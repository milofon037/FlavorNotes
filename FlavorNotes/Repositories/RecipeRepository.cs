using Microsoft.EntityFrameworkCore;
using FlavorNotes.Data;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.DTO;
using Dapper;
using Npgsql;

namespace FlavorNotes.Repositories;

public class RecipeRepository : IRecipeRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;

    public RecipeRepository(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<Recipe?> GetByIdAsync(int id)
    {
        return await _context.Recipes
            .Include(r => r.User)
            .Include(r => r.Category)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Unit)
            .Include(r => r.RecipeTags)
                .ThenInclude(rt => rt.Tag)
            .Include(r => r.InstructionSteps)
            .FirstOrDefaultAsync(r => r.RecipeId == id);
    }

    public async Task<PagedResponseDto<RecipeDto>> GetPagedAsync(int page, int pageSize, string? search)
    {
        var query = _context.Recipes
            .Include(r => r.User)
            .Include(r => r.Category)
            .Include(r => r.RecipeTags)
                .ThenInclude(rt => rt.Tag)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Title.Contains(search) || r.Description.Contains(search));
        }

        var total = await query.CountAsync();

        var recipes = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var recipeIds = recipes.Select(r => r.RecipeId).ToList();

        var recipeIngredients = await _context.RecipeIngredients
            .Include(ri => ri.Ingredient)
            .Include(ri => ri.Unit)
            .Where(ri => recipeIds.Contains(ri.RecipeId))
            .ToListAsync();

        var instructionSteps = await _context.InstructionSteps
            .Where(step => recipeIds.Contains(step.RecipeId))
            .OrderBy(step => step.StepNumber)
            .ToListAsync();

        var items = recipes.Select(r => new RecipeDto
        {
            RecipeId = r.RecipeId,
            UserId = r.UserId,
            Username = r.User.Username,
            CategoryId = r.CategoryId,
            CategoryName = r.Category.Name,
            Title = r.Title,
            Description = r.Description,
            PrepTimeMinutes = r.PrepTimeMinutes,
            CookTimeMinutes = r.CookTimeMinutes,
            Servings = r.Servings,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            Ingredients = recipeIngredients
                .Where(ri => ri.RecipeId == r.RecipeId)
                .Select(ri => new RecipeIngredientDto
                {
                    IngredientId = ri.IngredientId,
                    IngredientName = ri.Ingredient.Name,
                    UnitId = ri.UnitId,
                    UnitName = ri.Unit.Name,
                    UnitAbbreviation = ri.Unit.Abbreviation,
                    Quantity = ri.Quantity
                })
                .ToList(),
            Tags = r.RecipeTags.Select(rt => rt.Tag.Name).ToList(),
            Instructions = instructionSteps
                .Where(step => step.RecipeId == r.RecipeId)
                .OrderBy(step => step.StepNumber)
                .Select(step => new InstructionStepDto
                {
                    StepNumber = step.StepNumber,
                    InstructionText = step.InstructionText
                })
                .ToList()
        }).ToList();

        return new PagedResponseDto<RecipeDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Recipe> CreateAsync(Recipe recipe)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var recipeSql = @"
                INSERT INTO recipes (user_id, category_id, title, description, prep_time_minutes, cook_time_minutes, servings, created_at, updated_at)
                VALUES (@UserId, @CategoryId, @Title, @Description, @PrepTimeMinutes, @CookTimeMinutes, @Servings, @CreatedAt, @UpdatedAt)
                RETURNING recipe_id";

            var recipeId = await connection.QuerySingleAsync<int>(recipeSql, new
            {
                recipe.UserId,
                recipe.CategoryId,
                recipe.Title,
                recipe.Description,
                recipe.PrepTimeMinutes,
                recipe.CookTimeMinutes,
                recipe.Servings,
                recipe.CreatedAt,
                recipe.UpdatedAt
            }, transaction);

            recipe.RecipeId = recipeId;

            if (recipe.RecipeIngredients.Any())
            {
                var ingredientSql = @"
                    INSERT INTO recipe_ingredients (recipe_id, ingredient_id, unit_id, quantity)
                    VALUES (@RecipeId, @IngredientId, @UnitId, @Quantity)";

                await connection.ExecuteAsync(ingredientSql, recipe.RecipeIngredients.Select(ri => new
                {
                    RecipeId = recipeId,
                    ri.IngredientId,
                    ri.UnitId,
                    ri.Quantity
                }), transaction);
            }

            if (recipe.RecipeTags.Any())
            {
                var tagSql = @"
                    INSERT INTO recipe_tags (recipe_id, tag_id)
                    VALUES (@RecipeId, @TagId)";

                await connection.ExecuteAsync(tagSql, recipe.RecipeTags.Select(rt => new
                {
                    RecipeId = recipeId,
                    rt.TagId
                }), transaction);
            }

            if (recipe.InstructionSteps.Any())
            {
                var stepSql = @"
                    INSERT INTO instruction_steps (recipe_id, step_number, instruction_text)
                    VALUES (@RecipeId, @StepNumber, @InstructionText)";

                await connection.ExecuteAsync(stepSql, recipe.InstructionSteps.Select(step => new
                {
                    RecipeId = recipeId,
                    step.StepNumber,
                    step.InstructionText
                }), transaction);
            }

            await transaction.CommitAsync();
            return recipe;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Recipe> UpdateAsync(Recipe recipe)
    {
        _context.Recipes.Update(recipe);
        await _context.SaveChangesAsync();
        return recipe;
    }

    public async Task DeleteAsync(int id)
    {
        var recipe = await _context.Recipes.FindAsync(id);
        if (recipe != null)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SoftDeleteAsync(int id)
    {
        var recipe = await _context.Recipes.FindAsync(id);
        if (recipe != null)
        {
            recipe.DeletedAt = DateTime.UtcNow;
            _context.Recipes.Update(recipe);
            await _context.SaveChangesAsync();
        }
    }

    public async Task HardDeleteAsync(int id)
    {
        var recipe = await _context.Recipes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.RecipeId == id);
        if (recipe != null)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Recipes.AnyAsync(r => r.RecipeId == id);
    }

    public async Task<List<Recipe>> GetByUserAsync(int userId)
    {
        return await _context.Recipes
            .Where(r => r.UserId == userId)
            .Include(r => r.User)
            .Include(r => r.Category)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Unit)
            .Include(r => r.RecipeTags)
                .ThenInclude(rt => rt.Tag)
            .Include(r => r.InstructionSteps)
            .ToListAsync();
    }

    public async Task<List<Recipe>> GetFavoritesByUserAsync(int userId)
    {
        return await _context.Recipes
            .Where(r => _context.UserFavoriteRecipes
                .Any(ufr => ufr.UserId == userId && ufr.RecipeId == r.RecipeId))
            .Include(r => r.User)
            .Include(r => r.Category)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Unit)
            .Include(r => r.RecipeTags)
                .ThenInclude(rt => rt.Tag)
            .Include(r => r.InstructionSteps)
            .ToListAsync();
    }

    public async Task AddFavoriteAsync(int recipeId, int userId)
    {
        var existing = await _context.UserFavoriteRecipes
            .FirstOrDefaultAsync(ufr => ufr.UserId == userId && ufr.RecipeId == recipeId);

        if (existing == null)
        {
            _context.UserFavoriteRecipes.Add(new UserFavoriteRecipe
            {
                UserId = userId,
                RecipeId = recipeId,
                FavoritedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveFavoriteAsync(int recipeId, int userId)
    {
        var favorite = await _context.UserFavoriteRecipes
            .FirstOrDefaultAsync(ufr => ufr.UserId == userId && ufr.RecipeId == recipeId);

        if (favorite != null)
        {
            _context.UserFavoriteRecipes.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }
}

