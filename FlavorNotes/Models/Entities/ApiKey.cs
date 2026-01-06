namespace FlavorNotes.Models.Entities;

public class ApiKey
{
    public int ApiKeyId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

