using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> GetHealth()
    {
        var health = await _healthCheckService.CheckHealthAsync();
        
        if (health.Status == HealthStatus.Healthy)
        {
            return Ok(health);
        }
        
        return StatusCode(503, health);
    }
}

