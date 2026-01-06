namespace FlavorNotes.DTO;

public class RecipeDto
{
    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<InstructionStepDto> Instructions { get; set; } = new();
}
