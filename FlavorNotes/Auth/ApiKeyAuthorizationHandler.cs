using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FlavorNotes.Auth;

public class ApiKeyAuthorizationHandler : AuthorizationHandler<ApiKeyReadOnlyRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApiKeyReadOnlyRequirement requirement)
    {
        if (context.User.Identity?.AuthenticationType == "ApiKey")
        {
            var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
            if (httpContext != null && httpContext.Request.Method == "GET")
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

public class ApiKeyReadOnlyRequirement : IAuthorizationRequirement
{
}

