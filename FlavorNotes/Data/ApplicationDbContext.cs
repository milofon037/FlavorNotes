using Microsoft.EntityFrameworkCore;
using FlavorNotes.Models.Entities;

namespace FlavorNotes.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<RecipeTag> RecipeTags { get; set; }
    public DbSet<InstructionStep> InstructionSteps { get; set; }
    public DbSet<UserFavoriteRecipe> UserFavoriteRecipes { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50).HasDefaultValue("User");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("recipes");
            entity.HasKey(e => e.RecipeId);
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PrepTimeMinutes).HasColumnName("prep_time_minutes");
            entity.Property(e => e.CookTimeMinutes).HasColumnName("cook_time_minutes");
            entity.Property(e => e.Servings).HasColumnName("servings");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.HasOne(e => e.User).WithMany(u => u.Recipes).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Category).WithMany(c => c.Recipes).HasForeignKey(e => e.CategoryId);
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("ingredients");
            entity.HasKey(e => e.IngredientId);
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("units");
            entity.HasKey(e => e.UnitId);
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Abbreviation).HasColumnName("abbreviation").HasMaxLength(20);
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.ToTable("recipe_ingredients");
            entity.HasKey(e => new { e.RecipeId, e.IngredientId });
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasColumnType("decimal(10,2)");
            entity.HasOne(e => e.Recipe).WithMany(r => r.RecipeIngredients).HasForeignKey(e => e.RecipeId);
            entity.HasOne(e => e.Ingredient).WithMany(i => i.RecipeIngredients).HasForeignKey(e => e.IngredientId);
            entity.HasOne(e => e.Unit).WithMany(u => u.RecipeIngredients).HasForeignKey(e => e.UnitId);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(e => e.TagId);
            entity.Property(e => e.TagId).HasColumnName("tag_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<RecipeTag>(entity =>
        {
            entity.ToTable("recipe_tags");
            entity.HasKey(e => new { e.RecipeId, e.TagId });
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.TagId).HasColumnName("tag_id");
            entity.HasOne(e => e.Recipe).WithMany(r => r.RecipeTags).HasForeignKey(e => e.RecipeId);
            entity.HasOne(e => e.Tag).WithMany(t => t.RecipeTags).HasForeignKey(e => e.TagId);
        });

        modelBuilder.Entity<InstructionStep>(entity =>
        {
            entity.ToTable("instruction_steps");
            entity.HasKey(e => e.StepId);
            entity.Property(e => e.StepId).HasColumnName("step_id");
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.StepNumber).HasColumnName("step_number");
            entity.Property(e => e.InstructionText).HasColumnName("instruction_text");
            entity.HasOne(e => e.Recipe).WithMany(r => r.InstructionSteps).HasForeignKey(e => e.RecipeId);
        });

        modelBuilder.Entity<UserFavoriteRecipe>(entity =>
        {
            entity.ToTable("user_favorite_recipes");
            entity.HasKey(e => new { e.UserId, e.RecipeId });
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.FavoritedAt).HasColumnName("favorited_at");
            entity.HasOne(e => e.User).WithMany(u => u.FavoriteRecipes).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Recipe).WithMany(r => r.UserFavorites).HasForeignKey(e => e.RecipeId);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("api_keys");
            entity.HasKey(e => e.ApiKeyId);
            entity.Property(e => e.ApiKeyId).HasColumnName("api_key_id");
            entity.Property(e => e.Key).HasColumnName("key").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}

