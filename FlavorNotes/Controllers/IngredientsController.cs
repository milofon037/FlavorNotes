using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlavorNotes.DTO;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IngredientsController : ControllerBase
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly ILogger<IngredientsController> _logger;

    public IngredientsController(IIngredientRepository ingredientRepository, ILogger<IngredientsController> logger)
    {
        _ingredientRepository = ingredientRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IngredientDto>>> GetIngredients()
    {
        var ingredients = await _ingredientRepository.GetAllAsync();
        var dtos = ingredients.Select(i => new IngredientDto
        {
            IngredientId = i.IngredientId,
            Name = i.Name
        }).ToList();
        
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IngredientDto>> CreateIngredient([FromBody] IngredientDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Ingredient name is required" } });
        }

        var ingredient = new FlavorNotes.Models.Entities.Ingredient
        {
            Name = dto.Name
        };

        var created = await _ingredientRepository.CreateAsync(ingredient);
        _logger.LogInformation("Ingredient created: {IngredientName}", dto.Name);
        
        var result = new IngredientDto
        {
            IngredientId = created.IngredientId,
            Name = created.Name
        };
        
        return CreatedAtAction(nameof(GetIngredients), new { id = result.IngredientId }, result);
    }
}
