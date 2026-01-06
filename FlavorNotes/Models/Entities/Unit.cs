namespace FlavorNotes.Models.Entities;

public class Unit
{
    public int UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
}

