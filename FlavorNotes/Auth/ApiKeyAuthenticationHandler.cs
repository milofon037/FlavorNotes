using System.Security.Claims;
using System.Text.Encodings.Web;
using FlavorNotes.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FlavorNotes.Auth;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyRepository _apiKeyRepository;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyRepository apiKeyRepository)
        : base(options, logger, encoder)
    {
        _apiKeyRepository = apiKeyRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-KEY", out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(apiKey);
        if (apiKeyEntity == null || !apiKeyEntity.IsActive || apiKeyEntity.ExpiresAt <= DateTime.UtcNow)
        {
            return AuthenticateResult.Fail("Invalid or expired API key");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKey"),
            new Claim(ClaimTypes.Role, "ApiKey"),
            new Claim("ApiKeyId", apiKeyEntity.ApiKeyId.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}

