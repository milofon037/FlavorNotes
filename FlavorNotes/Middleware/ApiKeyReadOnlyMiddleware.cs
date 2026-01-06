namespace FlavorNotes.Middleware;

public class ApiKeyReadOnlyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyReadOnlyMiddleware> _logger;

    public ApiKeyReadOnlyMiddleware(RequestDelegate next, ILogger<ApiKeyReadOnlyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if user is authenticated via API Key
        if (context.User.Identity?.AuthenticationType == "ApiKey")
        {
            // API Key can only be used for GET requests
            if (context.Request.Method != "GET")
            {
                _logger.LogWarning("API Key attempted to use {Method} method on {Path}", 
                    context.Request.Method, context.Request.Path);
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = "FORBIDDEN",
                        message = "API Key authentication can only be used for GET requests"
                    }
                });
                return;
            }
        }

        await _next(context);
    }
}

