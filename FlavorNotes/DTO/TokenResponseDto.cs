namespace FlavorNotes.DTO;

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 86400;
    public string TokenType { get; set; } = "Bearer";
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

