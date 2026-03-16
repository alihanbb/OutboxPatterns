namespace OutboxPatterns.Domain;

public record UserCreatedEvent(Guid Id, string Name, string Email);

