using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FlavorNotes.DTO;
using FlavorNotes.Services.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<CategoryDto>>> GetCategories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _categoryService.GetPagedAsync(page, pageSize, search);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var result = await _categoryService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryDto dto)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        
        var result = await _categoryService.CreateAsync(dto, userRole);
        return CreatedAtAction(nameof(GetCategory), new { id = result.CategoryId }, result);
    }

    [HttpPut("{id}")]
    [HttpPatch("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] CategoryDto dto)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        
        var result = await _categoryService.UpdateAsync(id, dto, userRole);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        
        await _categoryService.DeleteAsync(id, userRole);
        return NoContent();
    }
}

