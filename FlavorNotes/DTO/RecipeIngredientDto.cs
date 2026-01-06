namespace FlavorNotes.DTO;

public class RecipeIngredientDto
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string? UnitAbbreviation { get; set; }
    public decimal Quantity { get; set; }
}
