using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Application;

public interface IClaimRepository
{
    Task CreateAsync(Claim claim, CancellationToken cancellationToken);

    Task<Claim?> GetByClaimIdAsync(string claimId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Claim> Items, long Total)> GetForUserAsync(string userId, int limit, int offset, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Claim> Items, long Total)> GetPendingAsync(int limit, int offset, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Claim> Items, long Total)> GetReviewedAsync(ClaimStatus? status, string? reviewerId, int limit, int offset, CancellationToken cancellationToken);

    Task<bool> ReplaceAsync(Claim claim, long expectedVersion, CancellationToken cancellationToken);

    Task<bool> ReplaceWithApprovalOutboxAsync(Claim claim, long expectedVersion, OutboxMessage outboxMessage, CancellationToken cancellationToken);
}
