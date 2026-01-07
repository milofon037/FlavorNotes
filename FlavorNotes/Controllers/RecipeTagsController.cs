using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FlavorNotes.Data;
using FlavorNotes.DTO;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/recipes/{recipeId}/tags")]
[Produces("application/json")]
public class RecipeTagsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRecipeRepository _recipeRepository;
    private readonly ILogger<RecipeTagsController> _logger;

    public RecipeTagsController(
        ApplicationDbContext context,
        IRecipeRepository recipeRepository,
        ILogger<RecipeTagsController> logger)
    {
        _context = context;
        _recipeRepository = recipeRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TagDto>>> GetRecipeTags(int recipeId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe not found" } });
        }

        var tags = await _context.RecipeTags
            .Include(rt => rt.Tag)
            .Where(rt => rt.RecipeId == recipeId)
            .Select(rt => new TagDto
            {
                TagId = rt.TagId,
                Name = rt.Tag.Name
            })
            .ToListAsync();

        return Ok(tags);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TagDto>> AddRecipeTag(int recipeId, [FromBody] TagDto dto)
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

        var tag = await _context.Tags.FindAsync(dto.TagId);
        if (tag == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Tag not found" } });
        }

        var existing = await _context.RecipeTags
            .FirstOrDefaultAsync(rt => rt.RecipeId == recipeId && rt.TagId == dto.TagId);

        if (existing != null)
        {
            return Ok(new TagDto { TagId = tag.TagId, Name = tag.Name });
        }

        _context.RecipeTags.Add(new Models.Entities.RecipeTag
        {
            RecipeId = recipeId,
            TagId = dto.TagId
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRecipeTags), new { recipeId }, new TagDto { TagId = tag.TagId, Name = tag.Name });
    }

    [HttpDelete("{tagId}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Manager,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteRecipeTag(int recipeId, int tagId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe not found" } });
        }

        var recipeTag = await _context.RecipeTags
            .FirstOrDefaultAsync(rt => rt.RecipeId == recipeId && rt.TagId == tagId);

        if (recipeTag == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe tag not found" } });
        }

        _context.RecipeTags.Remove(recipeTag);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


