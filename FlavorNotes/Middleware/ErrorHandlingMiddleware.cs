using System.Net;
using System.Text.Json;
using FlavorNotes.DTO;

namespace FlavorNotes.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = exception switch
        {
            KeyNotFoundException => HttpStatusCode.NotFound,
            UnauthorizedAccessException => HttpStatusCode.Forbidden,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        var response = new ErrorResponseDto
        {
            Error = code.ToString(),
            Message = exception.Message,
            TraceId = context.TraceIdentifier
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

