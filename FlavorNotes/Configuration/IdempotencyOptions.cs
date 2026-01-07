namespace FlavorNotes.Configuration;

public class IdempotencyOptions
{
    public const string SectionName = "Idempotency";
    
    public bool Enabled { get; set; } = true;
    public int CacheTtlHours { get; set; } = 24;
    public int KeyMaxLength { get; set; } = 128;
    public int KeyMinLength { get; set; } = 1;
    public bool ValidateRequestBody { get; set; } = false;
}

