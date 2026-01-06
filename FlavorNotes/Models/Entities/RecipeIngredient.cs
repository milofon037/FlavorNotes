namespace FlavorNotes.Models.Entities;

public class RecipeIngredient
{
    public int RecipeId { get; set; }
    public int IngredientId { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
    
    public Recipe Recipe { get; set; } = null!;
    public Ingredient Ingredient { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}

