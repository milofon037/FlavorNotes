using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace FlavorNotes.Swagger;

public class SwaggerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var allowAnonymous = context.MethodInfo.GetCustomAttributes(true)
            .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
            .Any() ||
            (context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
                .Any() ?? false);

        if (allowAnonymous)
        {
            return;
        }

        var methodAuthorize = context.MethodInfo.GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        var classAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        var authorizeAttribute = methodAuthorize ?? classAuthorize;

        if (authorizeAttribute != null)
        {
            var isGet = context.ApiDescription.HttpMethod?.Equals("GET", StringComparison.OrdinalIgnoreCase) ?? false;
            
            var authenticationSchemes = authorizeAttribute.AuthenticationSchemes ?? "";
            var allowsApiKey = isGet && authenticationSchemes.Contains("ApiKey", StringComparison.OrdinalIgnoreCase);
            
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

