using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FlavorNotes.DTO;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.Services.Interfaces;

namespace FlavorNotes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRecipeService _recipeService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository, 
        IRecipeService recipeService,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _recipeService = recipeService;
        _logger = logger;
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(new
        {
            userId = user.UserId,
            username = user.Username,
            email = user.Email,
            role = user.Role,
            createdAt = user.CreatedAt
        });
    }

    [HttpGet("me/recipes")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RecipeDto>>> GetMyRecipes()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _recipeService.GetByUserAsync(userId);
        return Ok(result);
    }

    [HttpGet("me/favorites")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RecipeDto>>> GetMyFavorites()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var favorites = await _recipeService.GetFavoritesByUserAsync(userId);
        return Ok(favorites);
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(new
        {
            userId = user.UserId,
            username = user.Username,
            email = user.Email,
            role = user.Role,
            createdAt = user.CreatedAt
        });
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateRoleDto dto)
    {
        var allowedRoles = new[] { "Admin", "Manager", "User" };
        if (!allowedRoles.Contains(dto.Role))
        {
            return BadRequest(new { error = "Invalid role. Allowed roles: Admin, Manager, User" });
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var oldRole = user.Role;
        user.Role = dto.Role;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Admin {AdminId} changed role for user {UserId} from {OldRole} to {NewRole}",
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id, oldRole, dto.Role);
        
        return Ok(new
        {
            message = "Role updated successfully",
            userId = user.UserId,
            username = user.Username,
            oldRole = oldRole,
            newRole = user.Role
        });
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userRepository.GetAllAsync();
        var result = users.Select(u => new
        {
            userId = u.UserId,
            username = u.Username,
            email = u.Email,
            role = u.Role,
            createdAt = u.CreatedAt
        }).ToList();

        return Ok(result);
    }
}

