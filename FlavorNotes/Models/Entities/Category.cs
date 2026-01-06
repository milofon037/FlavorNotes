namespace FlavorNotes.Models.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}

