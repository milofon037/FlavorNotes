using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace FlavorNotes.Swagger;

public class SwaggerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Any() ?? false;

        var methodAuthorize = context.MethodInfo.GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        var allowAnonymous = context.MethodInfo.GetCustomAttributes(true)
            .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
            .Any();

        if (allowAnonymous)
        {
            return;
        }

        if (hasAuthorize || methodAuthorize != null)
        {
            var isGet = context.ApiDescription.HttpMethod == "GET";
            
            // Check if endpoint allows ApiKey (for GET requests)
            // AuthenticationSchemes is a comma-separated string like "Bearer,ApiKey"
            var allowsApiKey = isGet && (methodAuthorize?.AuthenticationSchemes?.Contains("ApiKey") ?? false);
            
            // For GET requests that allow ApiKey, show both Bearer and ApiKey options
            // For other methods or GET without ApiKey, only show Bearer
            if (allowsApiKey)
            {
                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        },
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "ApiKey"
                                }
                            },
                            Array.Empty<string>()
                        }
                    }
                };
            }
            else
            {
                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    }
                };
            }
        }
    }
}

