using BCrypt.Net;
using FlavorNotes.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlavorNotes.Data;

public class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogError("Cannot connect to database. Please check connection string.");
                return;
            }

            _logger.LogInformation("Database connection successful");

            var hasUsers = false;
            try
            {
                hasUsers = await _context.Users.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if users exist. Will attempt to seed anyway.");
            }

            if (hasUsers)
            {
                _logger.LogInformation("Database already seeded - users exist");
                return;
            }

            _logger.LogInformation("Seeding database with test data");

        var admin = new User
        {
            Username = "admin",
            Email = "admin@flavornotes.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        var manager = new User
        {
            Username = "manager",
            Email = "manager@flavornotes.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"),
            Role = "Manager",
            CreatedAt = DateTime.UtcNow
        };

        var user1 = new User
        {
            Username = "john",
            Email = "john@flavornotes.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Username = "jane",
            Email = "jane@flavornotes.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(admin, manager, user1, user2);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} users", 4);

        var categories = new[]
        {
            new Category { Name = "Завтраки" },
            new Category { Name = "Обеды" },
            new Category { Name = "Ужины" },
            new Category { Name = "Десерты" },
            new Category { Name = "Напитки" },
            new Category { Name = "Закуски" }
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} categories", categories.Length);

        var tags = new[]
        {
            new Tag { Name = "Вегетарианское" },
            new Tag { Name = "Веганское" },
            new Tag { Name = "Без глютена" },
            new Tag { Name = "Быстрое" },
            new Tag { Name = "Полезное" },
            new Tag { Name = "Итальянское" },
            new Tag { Name = "Азиатское" },
            new Tag { Name = "Мексиканское" }
        };

        _context.Tags.AddRange(tags);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} tags", tags.Length);

        var ingredients = new[]
        {
            new Ingredient { Name = "Помидоры" },
            new Ingredient { Name = "Салат" },
            new Ingredient { Name = "Огурец" },
            new Ingredient { Name = "Лук" },
            new Ingredient { Name = "Чеснок" },
            new Ingredient { Name = "Макаронные изделия" },
            new Ingredient { Name = "Оливковое масло" },
            new Ingredient { Name = "Сыр пармезан" },
            new Ingredient { Name = "Сливки" },
            new Ingredient { Name = "Молоко" },
            new Ingredient { Name = "Яйца" },
            new Ingredient { Name = "Куриное филе" },
            new Ingredient { Name = "Говядина" },
            new Ingredient { Name = "Рис" },
            new Ingredient { Name = "Картофель" }
        };

        _context.Ingredients.AddRange(ingredients);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} ingredients", ingredients.Length);

        var units = new[]
        {
            new Unit { Name = "gram", Abbreviation = "g" },
            new Unit { Name = "milliliter", Abbreviation = "ml" },
            new Unit { Name = "tablespoon", Abbreviation = "tbsp" },
            new Unit { Name = "teaspoon", Abbreviation = "tsp" },
            new Unit { Name = "cup", Abbreviation = "cup" },
            new Unit { Name = "piece", Abbreviation = "pcs" }
        };

        _context.Units.AddRange(units);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} units", units.Length);

        var recipe1 = new Recipe
        {
            UserId = user1.UserId,
            CategoryId = categories[0].CategoryId,
            Title = "Паста Карбонара",
            Description = "Классический итальянский рецепт пасты со сливочным соусом",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Servings = 4,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RecipeIngredients = new List<RecipeIngredient>
            {
                new() { IngredientId = ingredients[5].IngredientId, UnitId = units[0].UnitId, Quantity = 400 },
                new() { IngredientId = ingredients[7].IngredientId, UnitId = units[0].UnitId, Quantity = 100 },
                new() { IngredientId = ingredients[8].IngredientId, UnitId = units[1].UnitId, Quantity = 200 }
            },
            RecipeTags = new List<RecipeTag>
            {
                new() { TagId = tags[5].TagId }
            },
            InstructionSteps = new List<InstructionStep>
            {
                new() { StepNumber = 1, InstructionText = "Отварите макаронные изделия в подсоленной воде" },
                new() { StepNumber = 2, InstructionText = "Смешайте сливки с пармезаном" },
                new() { StepNumber = 3, InstructionText = "Смешайте пасту со сливочным соусом" }
            }
        };

        var recipe2 = new Recipe
        {
            UserId = user2.UserId,
            CategoryId = categories[3].CategoryId,
            Title = "Шоколадный торт",
            Description = "Нежный шоколадный торт с глазурью",
            PrepTimeMinutes = 20,
            CookTimeMinutes = 30,
            Servings = 8,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RecipeIngredients = new List<RecipeIngredient>
            {
                new() { IngredientId = ingredients[10].IngredientId, UnitId = units[5].UnitId, Quantity = 3 },
                new() { IngredientId = ingredients[9].IngredientId, UnitId = units[1].UnitId, Quantity = 250 }
            },
            RecipeTags = new List<RecipeTag>
            {
                new() { TagId = tags[4].TagId }
            },
            InstructionSteps = new List<InstructionStep>
            {
                new() { StepNumber = 1, InstructionText = "Подготовьте ингредиенты" },
                new() { StepNumber = 2, InstructionText = "Смешайте мокрые и сухие ингредиенты" },
                new() { StepNumber = 3, InstructionText = "Выпекайте при 180 градусах 30 минут" }
            }
        };

        var recipe3 = new Recipe
        {
            UserId = user1.UserId,
            CategoryId = categories[1].CategoryId,
            Title = "Салат Цезарь",
            Description = "Классический салат с курицей и сухариками",
            PrepTimeMinutes = 15,
            CookTimeMinutes = 0,
            Servings = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RecipeIngredients = new List<RecipeIngredient>
            {
                new() { IngredientId = ingredients[1].IngredientId, UnitId = units[0].UnitId, Quantity = 200 },
                new() { IngredientId = ingredients[12].IngredientId, UnitId = units[0].UnitId, Quantity = 200 }
            },
            RecipeTags = new List<RecipeTag>
            {
                new() { TagId = tags[0].TagId },
                new() { TagId = tags[4].TagId }
            },
            InstructionSteps = new List<InstructionStep>
            {
                new() { StepNumber = 1, InstructionText = "Нарежьте салат" },
                new() { StepNumber = 2, InstructionText = "Приготовьте курицу" },
                new() { StepNumber = 3, InstructionText = "Смешайте ингредиенты" }
            }
        };

        _context.Recipes.AddRange(recipe1, recipe2, recipe3);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} recipes", 3);

        _context.UserFavoriteRecipes.AddRange(
            new UserFavoriteRecipe { UserId = user1.UserId, RecipeId = recipe2.RecipeId, FavoritedAt = DateTime.UtcNow },
            new UserFavoriteRecipe { UserId = user2.UserId, RecipeId = recipe1.RecipeId, FavoritedAt = DateTime.UtcNow }
        );

        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} favorite recipes", 2);

        var apiKeys = new[]
        {
            new ApiKey
            {
                Key = "test-api-key-readonly-12345",
                Name = "Test Read-Only API Key",
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow
            },
            new ApiKey
            {
                Key = "test-api-key-expired-67890",
                Name = "Expired API Key",
                IsActive = false,
                ExpiresAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            }
        };

        _context.ApiKeys.AddRange(apiKeys);
        await _context.SaveChangesAsync();
        _logger.LogInformation("API keys created: {Count}", apiKeys.Length);

        _logger.LogInformation("Database seeded successfully. Created: {UserCount} users, {CategoryCount} categories, {TagCount} tags, {IngredientCount} ingredients, {UnitCount} units, {RecipeCount} recipes",
            4, categories.Length, tags.Length, ingredients.Length, units.Length, 3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database: {Message}", ex.Message);
            throw;
        }
    }
}
