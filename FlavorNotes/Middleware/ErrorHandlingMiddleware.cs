using System.Net;
using System.Text.Json;
using FlavorNotes.DTO;
using Npgsql;

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
            await HandleExceptionAsync(context, ex, _logger);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        var (code, errorCode, message) = GetErrorInfo(exception, logger);

        var response = new
        {
            error = new
            {
                code = errorCode,
                message = message
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }

    private static (HttpStatusCode code, string errorCode, string message) GetErrorInfo(Exception exception, ILogger logger)
    {
        if (exception is PostgresException pgEx)
        {
            return HandlePostgresException(pgEx, logger);
        }

        if (exception.InnerException is PostgresException pgExInner)
        {
            return HandlePostgresException(pgExInner, logger);
        }

        return exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "FORBIDDEN", exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, "VALIDATION_ERROR", exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, "VALIDATION_ERROR", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", "An internal server error occurred")
        };
    }

    private static (HttpStatusCode code, string errorCode, string message) HandlePostgresException(PostgresException pgEx, ILogger logger)
    {
        if (pgEx.SqlState == "23503")
        {
            var message = GetForeignKeyErrorMessage(pgEx.ConstraintName ?? "");
            logger.LogWarning("Foreign key constraint violation: {ConstraintName}", pgEx.ConstraintName);
            return (HttpStatusCode.BadRequest, "NOT_FOUND", message);
        }

        logger.LogError(pgEx, "PostgreSQL error: {SqlState} - {MessageText}", pgEx.SqlState, pgEx.MessageText);
        return (HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", "An internal server error occurred");
    }

    private static string GetForeignKeyErrorMessage(string constraintName)
    {
        return constraintName switch
        {
            "fk_recipe_tags_tag_id" => "No tag with such tag_id",
            "fk_recipe_tags_recipe_id" => "No recipe with such recipe_id",
            "fk_recipe_ingredients_recipe_id" => "No recipe with such recipe_id",
            "fk_recipe_ingredients_ingredient_id" => "No ingredient with such ingredient_id",
            "fk_recipe_ingredients_unit_id" => "No unit with such unit_id",
            "fk_recipes_user_id" => "No user with such user_id",
            "fk_recipes_category_id" => "No category with such category_id",
            "fk_instruction_steps_recipe_id" => "No recipe with such recipe_id",
            "fk_user_favorite_recipes_user_id" => "No user with such user_id",
            "fk_user_favorite_recipes_recipe_id" => "No recipe with such recipe_id",
            _ => "Referenced resource not found"
        };
    }
}

