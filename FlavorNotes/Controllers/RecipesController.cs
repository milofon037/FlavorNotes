using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FlavorNotes.DTO;
using FlavorNotes.Services.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<RecipeDto>>> GetRecipes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _recipeService.GetPagedAsync(page, pageSize, search);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipeDto>> GetRecipe(int id)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var result = await _recipeService.GetByIdAsync(id, userRole, userId);
        if (result == null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RecipeDto>> CreateRecipe([FromBody] CreateRecipeDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        
        var result = await _recipeService.CreateAsync(dto, userId, userRole);
        return CreatedAtAction(nameof(GetRecipe), new { id = result.RecipeId }, result);
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RecipeDto>> UpdateRecipe(int id, [FromBody] UpdateRecipeDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        
        var result = await _recipeService.UpdateAsync(id, dto, userId, userRole);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteRecipe(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedAccessException("User role not found");
        
        await _recipeService.DeleteAsync(id, userId, userRole);
        return NoContent();
    }

    [HttpPost("{id}/favorite")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddFavorite(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _recipeService.AddFavoriteAsync(id, userId);
        return Ok(new { message = "Recipe added to favorites" });
    }

    [HttpDelete("{id}/favorite")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFavorite(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _recipeService.RemoveFavoriteAsync(id, userId);
        return NoContent();
    }
}

