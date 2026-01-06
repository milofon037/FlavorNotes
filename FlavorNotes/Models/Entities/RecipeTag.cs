namespace FlavorNotes.Models.Entities;

public class RecipeTag
{
    public int RecipeId { get; set; }
    public int TagId { get; set; }
    
    public Recipe Recipe { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

