namespace SharedEvent;

public record UserCreatedEvent(Guid Id, string Name, string Email);

