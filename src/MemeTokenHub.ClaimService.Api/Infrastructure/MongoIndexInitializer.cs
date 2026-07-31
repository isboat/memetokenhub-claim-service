using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Domain;
using MongoDB.Driver;

namespace MemeTokenHub.ClaimService.Api.Infrastructure;

public sealed class MongoIndexInitializer(IMongoDatabase database) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<Claim> claims = database.GetCollection<Claim>("Claims");
        CreateIndexModel<Claim>[] claimIndexes =
        [
            new(Builders<Claim>.IndexKeys.Ascending(claim => claim.ClaimId), new CreateIndexOptions { Unique = true }),
            new(Builders<Claim>.IndexKeys.Ascending(claim => claim.UserId).Descending(claim => claim.SubmittedAt)),
            new(Builders<Claim>.IndexKeys.Ascending(claim => claim.TokenId)),
            new(Builders<Claim>.IndexKeys.Ascending(claim => claim.Status).Descending(claim => claim.SubmittedAt))
        ];
        await claims.Indexes.CreateManyAsync(claimIndexes, cancellationToken);

        IMongoCollection<OutboxMessage> outbox = database.GetCollection<OutboxMessage>("Outbox");
        CreateIndexModel<OutboxMessage> outboxIndex = new(
            Builders<OutboxMessage>.IndexKeys.Ascending(message => message.PublishedAt).Ascending(message => message.OccurredAt));
        await outbox.Indexes.CreateOneAsync(outboxIndex, cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
