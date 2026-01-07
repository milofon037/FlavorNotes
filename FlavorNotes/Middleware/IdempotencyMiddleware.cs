using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlavorNotes.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Prometheus;

namespace FlavorNotes.Middleware;

public class IdempotencyMiddleware
{
    private static readonly Counter IdempotencyRequestsTotal = Metrics
        .CreateCounter("idempotency_requests_total", "Total number of idempotency requests", new[] { "status" });
    
    private static readonly Histogram IdempotencyRequestDuration = Metrics
        .CreateHistogram("idempotency_request_duration_seconds", "Idempotency request processing duration");

    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly IdempotencyOptions _options;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        IOptions<IdempotencyOptions> options,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        if (method != "POST" && method != "PUT" && method != "PATCH")
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyHeader))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = idempotencyKeyHeader.ToString();
        
        if (!ValidateIdempotencyKey(idempotencyKey, context))
        {
            return;
        }

        var requestBodyHash = string.Empty;
        if (_options.ValidateRequestBody)
        {
            requestBodyHash = await ComputeRequestBodyHashAsync(context);
        }

        var cacheKey = $"idempotency:{idempotencyKey}:{context.Request.Path}";
        if (_options.ValidateRequestBody)
        {
            cacheKey += $":{requestBodyHash}";
        }

        using var timer = IdempotencyRequestDuration.NewTimer();
        
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey);
            
            if (cached != null)
            {
                var response = JsonSerializer.Deserialize<IdempotencyResponse>(cached);
                if (response != null)
                {
                    if (_options.ValidateRequestBody && response.RequestBodyHash != requestBodyHash)
                    {
                        _logger.LogWarning(
                            "Idempotency key {Key} reused with different request body",
                            idempotencyKey);
                        context.Response.StatusCode = 422;
                        await WriteErrorResponseAsync(context, "Idempotency-Key already used with different request body");
                        IdempotencyRequestsTotal.WithLabels("conflict").Inc();
                        return;
                    }

                    _logger.LogInformation(
                        "Idempotency cache hit for key {Key}, method {Method}, path {Path}",
                        idempotencyKey, method, context.Request.Path);
                    
                    context.Response.StatusCode = response.StatusCode;
                    context.Response.ContentType = response.ContentType ?? "application/json";
                    
                    if (response.Headers != null)
                    {
                        foreach (var header in response.Headers)
                        {
                            if (!context.Response.Headers.ContainsKey(header.Key))
                            {
                                context.Response.Headers[header.Key] = header.Value;
                            }
                        }
                    }
                    
                    await context.Response.WriteAsync(response.Body);
                    IdempotencyRequestsTotal.WithLabels("cache_hit").Inc();
                    return;
                }
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
                Body = responseBodyText,
                ContentType = context.Response.ContentType,
                RequestBodyHash = requestBodyHash,
                Headers = context.Response.Headers
                    .Where(h => !h.Key.StartsWith("X-") && h.Key != "Content-Length" && h.Key != "Transfer-Encoding")
                    .ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            };

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                try
                {
                    var cachedAgain = await _cache.GetStringAsync(cacheKey);
                    if (cachedAgain == null)
                    {
                        await _cache.SetStringAsync(
                            cacheKey,
                            JsonSerializer.Serialize(idempotencyResponse),
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_options.CacheTtlHours)
                            });
                        
                        _logger.LogInformation(
                            "Idempotency response cached for key {Key}, method {Method}, path {Path}, status {Status}",
                            idempotencyKey, method, context.Request.Path, context.Response.StatusCode);
                        
                        IdempotencyRequestsTotal.WithLabels("cached").Inc();
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Idempotency response already cached by concurrent request for key {Key}",
                            idempotencyKey);
                        IdempotencyRequestsTotal.WithLabels("concurrent_cached").Inc();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cache idempotency response for key {Key}", idempotencyKey);
                    IdempotencyRequestsTotal.WithLabels("cache_error").Inc();
                }
            }
            else
            {
                _logger.LogWarning(
                    "Idempotency response not cached due to error status {Status} for key {Key}",
                    context.Response.StatusCode, idempotencyKey);
                IdempotencyRequestsTotal.WithLabels("error_not_cached").Inc();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing idempotency for key {Key}", idempotencyKey);
            IdempotencyRequestsTotal.WithLabels("error").Inc();
            await _next(context);
        }
    }

    private bool ValidateIdempotencyKey(string key, HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Empty Idempotency-Key header received");
            context.Response.StatusCode = 400;
            WriteErrorResponseAsync(context, "Idempotency-Key header is required and cannot be empty").Wait();
            IdempotencyRequestsTotal.WithLabels("invalid_key").Inc();
            return false;
        }

        if (key.Length < _options.KeyMinLength || key.Length > _options.KeyMaxLength)
        {
            _logger.LogWarning(
                "Idempotency-Key length {Length} is out of range [{Min}, {Max}]",
                key.Length, _options.KeyMinLength, _options.KeyMaxLength);
            context.Response.StatusCode = 400;
            WriteErrorResponseAsync(context, 
                $"Idempotency-Key length must be between {_options.KeyMinLength} and {_options.KeyMaxLength} characters").Wait();
            IdempotencyRequestsTotal.WithLabels("invalid_key").Inc();
            return false;
        }

        if (!IsValidKeyFormat(key))
        {
            _logger.LogWarning("Idempotency-Key contains invalid characters: {Key}", key);
            context.Response.StatusCode = 400;
            WriteErrorResponseAsync(context, "Idempotency-Key contains invalid characters").Wait();
            IdempotencyRequestsTotal.WithLabels("invalid_key").Inc();
            return false;
        }

        return true;
    }

    private static bool IsValidKeyFormat(string key)
    {
        return key.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.');
    }

    private static async Task<string> ComputeRequestBodyHashAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json";
        var errorResponse = JsonSerializer.Serialize(new
        {
            error = new
            {
                code = "VALIDATION_ERROR",
                message = message
            }
        });
        await context.Response.WriteAsync(errorResponse);
    }

    private class IdempotencyResponse
    {
        public int StatusCode { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public string RequestBodyHash { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
    }
}
