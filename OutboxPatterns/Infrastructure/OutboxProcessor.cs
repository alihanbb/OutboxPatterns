using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedEvent;
using System.Text.Json;

namespace OutboxPatterns.Infrastructure;

public class OutboxProcessor(IServiceScopeFactory serviceScopeFactory, 
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            await OutboxProcessorAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
    private async Task OutboxProcessorAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        
        var pendingMessages = await dbContext.OutboxTables
          .Where(x => x.ProcessedOn == null)
          .OrderBy(x => x.OccurredOn)
          .Take(20)
          .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                logger.LogInformation(
                   "Outbox mesajı işlendi: {EventType} | Id: {Id} | Payload: {Payload}",
                   message.EventType, message.Id, message.Payload);
                var userCreatedEvent = JsonSerializer.Deserialize<UserCreatedEvent>(message.Payload);

                await publishEndpoint.Publish(userCreatedEvent, cancellationToken);

                message.ProcessedOn = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                logger.LogError(ex, "Outbox mesajı işlenirken hata: {Id}", message.Id);
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
