using Azure.Messaging.ServiceBus;
using MemeTokenHub.ClaimService.Api.Application;
using MongoDB.Driver;

namespace MemeTokenHub.ClaimService.Api.Infrastructure;

public sealed partial class OutboxPublisher(
    IMongoDatabase database,
    ServiceBusSender serviceBusSender,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IMongoCollection<OutboxMessage> outbox = database.GetCollection<OutboxMessage>("Outbox");
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(5));
        do
        {
            await PublishBatchAsync(outbox, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PublishBatchAsync(IMongoCollection<OutboxMessage> outbox, CancellationToken cancellationToken)
    {
        List<OutboxMessage> messages = await outbox.Find(message => message.PublishedAt == null)
            .SortBy(message => message.OccurredAt)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (OutboxMessage message in messages)
        {
            try
            {
                ServiceBusMessage serviceBusMessage = new(message.Body)
                {
                    MessageId = message.EventId.ToString(),
                    Subject = message.EventType,
                    ContentType = "application/json"
                };
                await serviceBusSender.SendMessageAsync(serviceBusMessage, cancellationToken);
                UpdateDefinition<OutboxMessage> update = Builders<OutboxMessage>.Update
                    .Set(item => item.PublishedAt, DateTimeOffset.UtcNow)
                    .Inc(item => item.AttemptCount, 1);
                await outbox.UpdateOneAsync(item => item.EventId == message.EventId, update, cancellationToken: cancellationToken);
            }
            catch (ServiceBusException exception)
            {
                LogPublishFailure(logger, exception, message.EventId);
                await outbox.UpdateOneAsync(
                    item => item.EventId == message.EventId,
                    Builders<OutboxMessage>.Update.Inc(item => item.AttemptCount, 1),
                    cancellationToken: cancellationToken);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to publish outbox event {EventId}.")]
    private static partial void LogPublishFailure(ILogger logger, Exception exception, Guid eventId);
}
