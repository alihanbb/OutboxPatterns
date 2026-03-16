namespace OutboxPatterns.Domain;

public class OutboxTable
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;       
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }             
    public string? Error { get; set; }
}
