using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlavorNotes.DTO;
using FlavorNotes.Services.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IngredientsController : ControllerBase
{
    private readonly IIngredientService _ingredientService;

    public IngredientsController(IIngredientService ingredientService)
    {
        _ingredientService = ingredientService;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<IngredientDto>>> GetIngredients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _ingredientService.GetPagedAsync(page, pageSize, search);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IngredientDto>> CreateIngredient([FromBody] IngredientDto dto)
    {
        var result = await _ingredientService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetIngredients), new { id = result.IngredientId }, result);
    }
}
