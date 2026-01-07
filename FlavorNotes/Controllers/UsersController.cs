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
    private readonly IUserService _userService;
    private readonly IRecipeService _recipeService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IUserService userService,
        IRecipeService recipeService,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _userService = userService;
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
            return NotFound(new { error = new { code = "NOT_FOUND", message = "User not found" } });
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

    [HttpPut("me")]
    [HttpPatch("me")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "User not found" } });
        }

        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.Username)
        {
            if (await _userRepository.UsernameExistsAsync(dto.Username))
            {
                return BadRequest(new { error = new { code = "CONFLICT", message = "Username already exists" } });
            }
            user.Username = dto.Username;
        }

        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
        {
            if (await _userRepository.EmailExistsAsync(dto.Email))
            {
                return BadRequest(new { error = new { code = "CONFLICT", message = "Email already exists" } });
            }
            user.Email = dto.Email;
        }

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("User {UserId} updated their profile", userId);

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

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponseDto<UserDto>>> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _userService.GetPagedAsync(page, pageSize, search);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "User not found" } });
        }

        return Ok(user);
    }

    [HttpPut("{id}")]
    [HttpPatch("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateRoleDto dto)
    {
        var adminRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        var result = await _userService.UpdateRoleAsync(id, dto, adminRole);
        
        return Ok(new
        {
            message = "Role updated successfully",
            userId = result.UserId,
            username = result.Username,
            role = result.Role
        });
    }
}

