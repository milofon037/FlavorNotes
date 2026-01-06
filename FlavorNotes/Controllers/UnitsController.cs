using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlavorNotes.DTO;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UnitsController : ControllerBase
{
    private readonly IUnitRepository _unitRepository;
    private readonly ILogger<UnitsController> _logger;

    public UnitsController(IUnitRepository unitRepository, ILogger<UnitsController> logger)
    {
        _unitRepository = unitRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UnitDto>>> GetUnits()
    {
        var units = await _unitRepository.GetAllAsync();
        var dtos = units.Select(u => new UnitDto
        {
            UnitId = u.UnitId,
            Name = u.Name,
            Abbreviation = u.Abbreviation
        }).ToList();
        
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnitDto>> CreateUnit([FromBody] UnitDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Unit name is required" } });
        }

        var unit = new FlavorNotes.Models.Entities.Unit
        {
            Name = dto.Name,
            Abbreviation = dto.Abbreviation
        };

        var created = await _unitRepository.CreateAsync(unit);
        _logger.LogInformation("Unit created: {UnitName}", dto.Name);
        
        var result = new UnitDto
        {
            UnitId = created.UnitId,
            Name = created.Name,
            Abbreviation = created.Abbreviation
        };
        
        return CreatedAtAction(nameof(GetUnits), new { id = result.UnitId }, result);
    }
}
