namespace FlavorNotes.Models.Entities;

public class UserFavoriteRecipe
{
    public int UserId { get; set; }
    public int RecipeId { get; set; }
    public DateTime FavoritedAt { get; set; }
    
    public User User { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}

