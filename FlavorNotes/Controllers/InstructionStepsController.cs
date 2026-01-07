using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FlavorNotes.Data;
using FlavorNotes.DTO;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/recipes/{recipeId}/steps")]
[Produces("application/json")]
public class InstructionStepsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRecipeRepository _recipeRepository;
    private readonly ILogger<InstructionStepsController> _logger;

    public InstructionStepsController(
        ApplicationDbContext context,
        IRecipeRepository recipeRepository,
        ILogger<InstructionStepsController> logger)
    {
        _context = context;
        _recipeRepository = recipeRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<InstructionStepDto>>> GetInstructionSteps(int recipeId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe not found" } });
        }

        var steps = await _context.InstructionSteps
            .Where(step => step.RecipeId == recipeId)
            .OrderBy(step => step.StepNumber)
            .Select(step => new InstructionStepDto
            {
                StepNumber = step.StepNumber,
                InstructionText = step.InstructionText
            })
            .ToListAsync();

        return Ok(steps);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InstructionStepDto>> AddInstructionStep(
        int recipeId,
        [FromBody] InstructionStepDto dto)
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

        var step = new Models.Entities.InstructionStep
        {
            RecipeId = recipeId,
            StepNumber = dto.StepNumber,
            InstructionText = dto.InstructionText
        };

        _context.InstructionSteps.Add(step);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInstructionSteps), new { recipeId }, dto);
    }

    [HttpPut("{stepId}")]
    [HttpPatch("{stepId}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InstructionStepDto>> UpdateInstructionStep(
        int recipeId,
        int stepId,
        [FromBody] InstructionStepDto dto)
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

        var step = await _context.InstructionSteps
            .FirstOrDefaultAsync(s => s.StepId == stepId && s.RecipeId == recipeId);

        if (step == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Instruction step not found" } });
        }

        step.StepNumber = dto.StepNumber;
        step.InstructionText = dto.InstructionText;

        await _context.SaveChangesAsync();

        return Ok(dto);
    }

    [HttpDelete("{stepId}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Manager,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteInstructionStep(int recipeId, int stepId)
    {
        var recipe = await _recipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Recipe not found" } });
        }

        var step = await _context.InstructionSteps
            .FirstOrDefaultAsync(s => s.StepId == stepId && s.RecipeId == recipeId);

        if (step == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Instruction step not found" } });
        }

        _context.InstructionSteps.Remove(step);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

