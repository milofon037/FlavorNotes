using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using FlavorNotes.DTO;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace FlavorNotes.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TokenResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await _userRepository.UsernameExistsAsync(dto.Username))
        {
            throw new InvalidOperationException("Username already exists");
        }

        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);
        _logger.LogInformation("User {Username} registered", dto.Username);

        return GenerateToken(user);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        _logger.LogInformation("User {Username} logged in", dto.Username);

        return GenerateToken(user);
    }

    private TokenResponseDto GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new TokenResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            Role = user.Role,
            UserId = user.UserId,
            Username = user.Username,
            ExpiresIn = 86400,
            TokenType = "Bearer"
        };
    }
}

