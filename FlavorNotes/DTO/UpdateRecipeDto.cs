namespace FlavorNotes.DTO;

public class UpdateRecipeDto
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public List<CreateRecipeIngredientDto> Ingredients { get; set; } = new();
    public List<int> TagIds { get; set; } = new();
    public List<CreateInstructionStepDto> Instructions { get; set; } = new();
}

