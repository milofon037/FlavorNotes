namespace FlavorNotes.DTO;

public class CreateRecipeDto
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

public class CreateRecipeIngredientDto
{
    public int IngredientId { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
}

public class CreateInstructionStepDto
{
    public int StepNumber { get; set; }
    public string InstructionText { get; set; } = string.Empty;
}

