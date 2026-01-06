using FlavorNotes.DTO;

namespace FlavorNotes.Services.Interfaces;

public interface IAuthService
{
    Task<TokenResponseDto> RegisterAsync(RegisterDto dto);
    Task<TokenResponseDto> LoginAsync(LoginDto dto);
}

