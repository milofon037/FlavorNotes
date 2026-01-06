namespace FlavorNotes.Models.Entities;

public class Recipe
{
    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
    
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RecipeTag> RecipeTags { get; set; } = new List<RecipeTag>();
    public ICollection<InstructionStep> InstructionSteps { get; set; } = new List<InstructionStep>();
    public ICollection<UserFavoriteRecipe> UserFavorites { get; set; } = new List<UserFavoriteRecipe>();
}

