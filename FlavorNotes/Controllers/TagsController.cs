using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlavorNotes.DTO;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TagsController : ControllerBase
{
    private readonly ITagRepository _tagRepository;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagRepository tagRepository, ILogger<TagsController> logger)
    {
        _tagRepository = tagRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TagDto>>> GetTags()
    {
        var tags = await _tagRepository.GetAllAsync();
        var dtos = tags.Select(t => new TagDto
        {
            TagId = t.TagId,
            Name = t.Name
        }).ToList();
        
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] TagDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Tag name is required" } });
        }

        var tag = new FlavorNotes.Models.Entities.Tag
        {
            Name = dto.Name
        };

        var created = await _tagRepository.CreateAsync(tag);
        _logger.LogInformation("Tag created: {TagName}", dto.Name);
        
        var result = new TagDto
        {
            TagId = created.TagId,
            Name = created.Name
        };
        
        return CreatedAtAction(nameof(GetTags), new { id = result.TagId }, result);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Tag not found" } });
        }

        await _tagRepository.DeleteAsync(id);
        _logger.LogInformation("Tag {TagId} deleted by admin", id);
        return NoContent();
    }
}
