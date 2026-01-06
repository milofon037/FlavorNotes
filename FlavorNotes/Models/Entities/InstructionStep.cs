namespace FlavorNotes.Models.Entities;

public class InstructionStep
{
    public int StepId { get; set; }
    public int RecipeId { get; set; }
    public int StepNumber { get; set; }
    public string InstructionText { get; set; } = string.Empty;
    
    public Recipe Recipe { get; set; } = null!;
}

