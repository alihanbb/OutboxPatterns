using MassTransit;
using Microsoft.Extensions.Logging;
using SharedEvent;

public class UserCreatedEventConsumer(ILogger<UserCreatedEventConsumer> logger)
    : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        logger.LogInformation(
            "UserCreated event alındı: Id={Id}, Name={Name}, Email={Email}",
            context.Message.Id,
            context.Message.Name,
            context.Message.Email);

        await Task.CompletedTask;
    }
}
