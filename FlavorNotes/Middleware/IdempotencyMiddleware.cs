using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace FlavorNotes.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;

    public IdempotencyMiddleware(RequestDelegate next, IDistributedCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method != "POST")
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey))
        {
            await _next(context);
            return;
        }

        var key = $"idempotency:{idempotencyKey}";
        var cached = await _cache.GetStringAsync(key);
        
        if (cached != null)
        {
            var response = System.Text.Json.JsonSerializer.Deserialize<IdempotencyResponse>(cached);
            context.Response.StatusCode = response!.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(response.Body);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);

        var idempotencyResponse = new IdempotencyResponse
        {
            StatusCode = context.Response.StatusCode,
            Body = responseBodyText
        };

        await _cache.SetStringAsync(key, System.Text.Json.JsonSerializer.Serialize(idempotencyResponse), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });
    }

    private class IdempotencyResponse
    {
        public int StatusCode { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}

