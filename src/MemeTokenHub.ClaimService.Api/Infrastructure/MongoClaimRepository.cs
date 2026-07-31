using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Domain;
using MongoDB.Driver;

namespace MemeTokenHub.ClaimService.Api.Infrastructure;

public sealed class MongoClaimRepository : IClaimRepository
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoCollection<Claim> _claims;
    private readonly IMongoCollection<OutboxMessage> _outbox;

    public MongoClaimRepository(IMongoClient mongoClient, IMongoDatabase database)
    {
        _mongoClient = mongoClient;
        _claims = database.GetCollection<Claim>("Claims");
        _outbox = database.GetCollection<OutboxMessage>("Outbox");
    }

    public Task CreateAsync(Claim claim, CancellationToken cancellationToken) =>
        _claims.InsertOneAsync(claim, cancellationToken: cancellationToken);

    public async Task<Claim?> GetByClaimIdAsync(string claimId, CancellationToken cancellationToken) =>
        await _claims.Find(claim => claim.ClaimId == claimId).FirstOrDefaultAsync(cancellationToken);

    public Task<(IReadOnlyList<Claim> Items, long Total)> GetForUserAsync(
        string userId,
        int limit,
        int offset,
        CancellationToken cancellationToken) =>
        GetPageAsync(Builders<Claim>.Filter.Eq(claim => claim.UserId, userId), limit, offset, cancellationToken);

    public Task<(IReadOnlyList<Claim> Items, long Total)> GetPendingAsync(int limit, int offset, CancellationToken cancellationToken) =>
        GetPageAsync(Builders<Claim>.Filter.Eq(claim => claim.Status, ClaimStatus.Pending), limit, offset, cancellationToken);

    public Task<(IReadOnlyList<Claim> Items, long Total)> GetReviewedAsync(
        ClaimStatus? status,
        string? reviewerId,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<Claim> filters = Builders<Claim>.Filter;
        FilterDefinition<Claim> filter = filters.Ne(claim => claim.Status, ClaimStatus.Pending);
        if (status is not null)
        {
            filter &= filters.Eq(claim => claim.Status, status.Value);
        }

        if (!string.IsNullOrWhiteSpace(reviewerId))
        {
            filter &= filters.Eq(claim => claim.ReviewerId, reviewerId);
        }

        return GetPageAsync(filter, limit, offset, cancellationToken);
    }

    public async Task<bool> ReplaceAsync(Claim claim, long expectedVersion, CancellationToken cancellationToken)
    {
        FilterDefinition<Claim> filter = Builders<Claim>.Filter.And(
            Builders<Claim>.Filter.Eq(item => item.ClaimId, claim.ClaimId),
            Builders<Claim>.Filter.Eq(item => item.Version, expectedVersion));
        ReplaceOneResult result = await _claims.ReplaceOneAsync(filter, claim, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> ReplaceWithApprovalOutboxAsync(
        Claim claim,
        long expectedVersion,
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken)
    {
        using IClientSessionHandle session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();
        try
        {
            FilterDefinition<Claim> filter = Builders<Claim>.Filter.And(
                Builders<Claim>.Filter.Eq(item => item.ClaimId, claim.ClaimId),
                Builders<Claim>.Filter.Eq(item => item.Version, expectedVersion));
            ReplaceOneResult result = await _claims.ReplaceOneAsync(session, filter, claim, cancellationToken: cancellationToken);
            if (result.ModifiedCount != 1)
            {
                await session.AbortTransactionAsync(cancellationToken);
                return false;
            }

            await _outbox.InsertOneAsync(session, outboxMessage, cancellationToken: cancellationToken);
            await session.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<(IReadOnlyList<Claim> Items, long Total)> GetPageAsync(
        FilterDefinition<Claim> filter,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        long total = await _claims.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<Claim> claims = await _claims.Find(filter)
            .SortByDescending(claim => claim.SubmittedAt)
            .Skip(offset)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return (claims, total);
    }
}
