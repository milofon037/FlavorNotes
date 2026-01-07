using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FlavorNotes.Data;
using FlavorNotes.DTO;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/recipes/{recipeId}/ingredients")]
[Produces("application/json")]
public class RecipeIngredientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRecipeRepository _recipeRepository;
    private readonly ILogger<RecipeIngredientsController> _logger;

    public RecipeIngredientsController(
        ApplicationDbContext context,
        IRecipeRepository recipeRepository,
        ILogger<RecipeIngredientsController> logger)
    {
        _context = context;
        _recipeRepository = recipeRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<RecipeIngredientDto>>> GetRecipeIngredients(int recipeId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe not found" } });
        }

        var ingredients = await _context.RecipeIngredients
            .Include(ri => ri.Ingredient)
            .Include(ri => ri.Unit)
            .Where(ri => ri.RecipeId == recipeId)
            .Select(ri => new RecipeIngredientDto
            {
                IngredientId = ri.IngredientId,
                IngredientName = ri.Ingredient.Name,
                UnitId = ri.UnitId,
                UnitName = ri.Unit.Name,
                UnitAbbreviation = ri.Unit.Abbreviation,
                Quantity = ri.Quantity
            })
            .ToListAsync();

        return Ok(ingredients);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RecipeIngredientDto>> AddRecipeIngredient(
        int recipeId,
        [FromBody] RecipeIngredientDto dto)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe not found" } });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

        if (userRole == "User" && recipe.UserId != userId)
        {
            return StatusCode(403, new { error = new { code = "FORBIDDEN", message = "You can only modify your own recipes" } });
        }

        var existing = await _context.RecipeIngredients
            .FirstOrDefaultAsync(ri => ri.RecipeId == recipeId && ri.IngredientId == dto.IngredientId);

        if (existing != null)
        {
            existing.UnitId = dto.UnitId;
            existing.Quantity = dto.Quantity;
        }
        else
        {
            _context.RecipeIngredients.Add(new Models.Entities.RecipeIngredient
            {
                RecipeId = recipeId,
                IngredientId = dto.IngredientId,
                UnitId = dto.UnitId,
                Quantity = dto.Quantity
            });
        }

        await _context.SaveChangesAsync();

        var ingredient = await _context.Ingredients.FindAsync(dto.IngredientId);
        var unit = await _context.Units.FindAsync(dto.UnitId);

        var result = new RecipeIngredientDto
        {
            IngredientId = dto.IngredientId,
            IngredientName = ingredient?.Name ?? "",
            UnitId = dto.UnitId,
            UnitName = unit?.Name ?? "",
            UnitAbbreviation = unit?.Abbreviation,
            Quantity = dto.Quantity
        };

        return CreatedAtAction(nameof(GetRecipeIngredients), new { recipeId }, result);
    }

    [HttpPut("{ingredientId}")]
    [HttpPatch("{ingredientId}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RecipeIngredientDto>> UpdateRecipeIngredient(
        int recipeId,
        int ingredientId,
        [FromBody] RecipeIngredientDto dto)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe not found" } });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

        if (userRole == "User" && recipe.UserId != userId)
        {
            return StatusCode(403, new { error = new { code = "FORBIDDEN", message = "You can only modify your own recipes" } });
        }

        var recipeIngredient = await _context.RecipeIngredients
            .FirstOrDefaultAsync(ri => ri.RecipeId == recipeId && ri.IngredientId == ingredientId);

        if (recipeIngredient == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe ingredient not found" } });
        }

        recipeIngredient.UnitId = dto.UnitId;
        recipeIngredient.Quantity = dto.Quantity;

        await _context.SaveChangesAsync();

        var ingredient = await _context.Ingredients.FindAsync(ingredientId);
        var unit = await _context.Units.FindAsync(dto.UnitId);

        return Ok(new RecipeIngredientDto
        {
            IngredientId = ingredientId,
            IngredientName = ingredient?.Name ?? "",
            UnitId = dto.UnitId,
            UnitName = unit?.Name ?? "",
            UnitAbbreviation = unit?.Abbreviation,
            Quantity = dto.Quantity
        });
    }

    [HttpDelete("{ingredientId}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Manager,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteRecipeIngredient(int recipeId, int ingredientId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe not found" } });
        }

        var recipeIngredient = await _context.RecipeIngredients
            .FirstOrDefaultAsync(ri => ri.RecipeId == recipeId && ri.IngredientId == ingredientId);

        if (recipeIngredient == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe ingredient not found" } });
        }

        _context.RecipeIngredients.Remove(recipeIngredient);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


