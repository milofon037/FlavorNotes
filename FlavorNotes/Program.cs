using System.Text;
using FluentValidation;
using FlavorNotes.Auth;
using FlavorNotes.Data;
using FlavorNotes.Middleware;
using FlavorNotes.Repositories;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.Services;
using FlavorNotes.Services.Interfaces;
using FlavorNotes.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using AspNetCoreRateLimit;
using Serilog;
using Microsoft.AspNetCore.Authentication;
using StackExchange.Redis;

// Создаём logs директорию если её нет
var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
if (!Directory.Exists(logsPath))
{
    Directory.CreateDirectory(logsPath);
}

// Configure Serilog ПЕРЕД созданием WebApplication
try
{
    var builder = WebApplication.CreateBuilder(args);

    // Setup Serilog from configuration
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "FlavorNotes")
        .CreateLogger();

    builder.Host.UseSerilog(Log.Logger);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "FlavorNotes API",
            Version = "v1",
            Description = "Recipe Book REST API"
        });
        
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        
        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = "API Key authentication (read-only, GET requests only)",
            Name = "X-API-KEY",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
        });
        
        c.OperationFilter<FlavorNotes.Swagger.SwaggerSecurityOperationFilter>();
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

var redisConnection = builder.Configuration.GetConnectionString("Redis");
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnection!));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
})
.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("ApiKeyReadOnly", policy => 
        policy.Requirements.Add(new FlavorNotes.Auth.ApiKeyReadOnlyRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, FlavorNotes.Auth.ApiKeyAuthorizationHandler>();

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!)
    .AddRedis(redisConnection!);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IDataSeeder, DataSeeder>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateRecipeDtoValidator>();

var app = builder.Build();

// Логируем что приложение стартует
Log.Information("FlavorNotes API starting...");

// Seeding
try
{
    Log.Information("Creating service scope for seeding...");
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Ensuring database is created...");
        await dbContext.Database.EnsureCreatedAsync();
        
        logger.LogInformation("Running pending migrations...");
        await dbContext.Database.MigrateAsync();
        
        logger.LogInformation("Starting database seeding...");
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
        
        logger.LogInformation("Database seeding completed successfully");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "An error occurred while seeding the database: {Message}", ex.Message);
    // Don't throw - allow app to start even if seeding fails
}

Log.Information("Building middleware pipeline...");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlavorNotes API v1");
});

app.UseHttpMetrics();
app.UseMetricServer();

app.UseIpRateLimiting();

app.UseMiddleware<IdempotencyMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseMiddleware<FlavorNotes.Middleware.ApiKeyReadOnlyMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

Log.Information("Starting application...");
app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
