namespace FlavorNotes.Models.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; }
    
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public ICollection<UserFavoriteRecipe> FavoriteRecipes { get; set; } = new List<UserFavoriteRecipe>();
}

