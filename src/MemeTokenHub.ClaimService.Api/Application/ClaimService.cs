using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.ClaimService.Api.Dtos;

namespace MemeTokenHub.ClaimService.Api.Application;

/// <summary>
/// Coordinates claim validation, persistence, moderation, appeals, and event creation.
/// </summary>
public sealed class ClaimService(
    IClaimRepository claimRepository,
    IReferenceValidationService referenceValidationService,
    IAttachmentService attachmentService,
    IEventEnvelopeFactory eventEnvelopeFactory,
    TimeProvider timeProvider) : IClaimService
{
    /// <summary>
    /// Validates referenced resources and stores an immutable claim submission snapshot.
    /// </summary>
    public async Task<ClaimSummaryResponse> SubmitAsync(
        string userId,
        SubmitClaimRequest request,
        CancellationToken cancellationToken)
    {
        ValidateClaimType(request.Type);
        ValidateProofMethod(request.Proof.Method);
        string description = NormalizeRequiredText(request.Description, nameof(request.Description), 10);
        await referenceValidationService.ValidateClaimantAsync(userId, cancellationToken);
        await referenceValidationService.ValidateTokenAsync(request.TokenId, cancellationToken);
        await attachmentService.ValidateReferencesAsync(userId, request.Attachments, cancellationToken);

        DateTimeOffset submittedAt = timeProvider.GetUtcNow();
        Claim claim = new()
        {
            ClaimId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            TokenId = request.TokenId,
            Type = request.Type,
            Description = description,
            Attachments = request.Attachments.ToArray(),
            Proof = request.Proof.ToSnapshot(),
            SubmittedAt = submittedAt,
            AuditHistory =
            [
                new ClaimAuditEntry
                {
                    Action = "Submitted",
                    ActorId = userId,
                    OccurredAt = submittedAt
                }
            ]
        };

        await claimRepository.CreateAsync(claim, cancellationToken);
        return claim.ToSummary();
    }

    /// <summary>
    /// Returns the redacted claim history belonging to a user.
    /// </summary>
    public async Task<PagedResponse<ClaimSummaryResponse>> GetForUserAsync(
        string userId,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        (IReadOnlyList<Claim> claims, long total) = await claimRepository.GetForUserAsync(userId, limit, offset, cancellationToken);
        return new PagedResponse<ClaimSummaryResponse>(claims.Select(ClaimMapper.ToSummary).ToArray(), limit, offset, total);
    }

    /// <summary>
    /// Returns private pending claims for authorized moderators.
    /// </summary>
    public async Task<PagedResponse<ClaimModeratorResponse>> GetPendingAsync(int limit, int offset, CancellationToken cancellationToken)
    {
        (IReadOnlyList<Claim> claims, long total) = await claimRepository.GetPendingAsync(limit, offset, cancellationToken);
        return new PagedResponse<ClaimModeratorResponse>(claims.Select(ClaimMapper.ToModeratorResponse).ToArray(), limit, offset, total);
    }

    /// <summary>
    /// Returns private reviewed-claim audit information for authorized moderators.
    /// </summary>
    public async Task<PagedResponse<ClaimModeratorResponse>> GetReviewedAsync(
        ClaimStatus? status,
        string? reviewerId,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        (IReadOnlyList<Claim> claims, long total) = await claimRepository.GetReviewedAsync(status, reviewerId, limit, offset, cancellationToken);
        return new PagedResponse<ClaimModeratorResponse>(claims.Select(ClaimMapper.ToModeratorResponse).ToArray(), limit, offset, total);
    }

    /// <summary>
    /// Returns only fields approved for public verification badges.
    /// </summary>
    public async Task<PublicClaimStatusResponse> GetPublicStatusAsync(string claimId, CancellationToken cancellationToken)
    {
        Claim claim = await GetRequiredClaimAsync(claimId, cancellationToken);
        return claim.ToPublicStatus();
    }

    /// <summary>
    /// Applies a moderator decision using optimistic concurrency and atomically queues approval events.
    /// </summary>
    public async Task<ClaimSummaryResponse> ReviewAsync(
        string claimId,
        string reviewerId,
        ReviewClaimRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        Claim claim = await GetRequiredClaimAsync(claimId, cancellationToken);
        long previousVersion = claim.Version;
        if (previousVersion != request.ExpectedVersion)
        {
            throw new ClaimConcurrencyException();
        }

        string reviewNotes = NormalizeRequiredText(request.Notes, nameof(request.Notes), 1);
        string reasonCode = NormalizeRequiredText(request.ReasonCode, nameof(request.ReasonCode), 1);
        claim.Review(request.Status, reviewerId, reviewNotes, reasonCode, timeProvider.GetUtcNow());
        bool updated;
        if (claim.Status == ClaimStatus.Approved)
        {
            OutboxMessage outboxMessage = eventEnvelopeFactory.CreateClaimApproved(claim, correlationId);
            updated = await claimRepository.ReplaceWithApprovalOutboxAsync(claim, previousVersion, outboxMessage, cancellationToken);
        }
        else
        {
            updated = await claimRepository.ReplaceAsync(claim, previousVersion, cancellationToken);
        }

        if (!updated)
        {
            throw new ClaimConcurrencyException();
        }

        return claim.ToSummary();
    }

    /// <summary>
    /// Reopens a rejected claim with the owner's single permitted appeal.
    /// </summary>
    public async Task<ClaimSummaryResponse> AppealAsync(
        string claimId,
        string userId,
        AppealClaimRequest request,
        CancellationToken cancellationToken)
    {
        Claim claim = await GetRequiredClaimAsync(claimId, cancellationToken);
        if (!string.Equals(claim.UserId, userId, StringComparison.Ordinal))
        {
            throw new ClaimAccessDeniedException();
        }

        ValidateProofMethod(request.Proof.Method);
        string appealReason = NormalizeRequiredText(request.Reason, nameof(request.Reason), 10);
        await attachmentService.ValidateReferencesAsync(userId, request.Attachments, cancellationToken);
        long previousVersion = claim.Version;
        ClaimAppeal appeal = new()
        {
            Reason = appealReason,
            Proof = request.Proof.ToSnapshot(),
            Attachments = request.Attachments.ToArray(),
            SubmittedAt = timeProvider.GetUtcNow()
        };
        claim.SubmitAppeal(appeal, userId);

        if (!await claimRepository.ReplaceAsync(claim, previousVersion, cancellationToken))
        {
            throw new ClaimConcurrencyException();
        }

        return claim.ToSummary();
    }

    private async Task<Claim> GetRequiredClaimAsync(string claimId, CancellationToken cancellationToken) =>
        await claimRepository.GetByClaimIdAsync(claimId, cancellationToken)
        ?? throw new EntityNotFoundException($"Claim '{claimId}' was not found.");

    private static void ValidateClaimType(ClaimType claimType)
    {
        if (!Enum.IsDefined(claimType))
        {
            throw new ArgumentException("The claim type is not defined.", nameof(claimType));
        }
    }

    private static void ValidateProofMethod(ProofMethod proofMethod)
    {
        if (!Enum.IsDefined(proofMethod))
        {
            throw new ArgumentException("The proof method is not defined.", nameof(proofMethod));
        }
    }

    private static string NormalizeRequiredText(string value, string parameterName, int minimumLength)
    {
        string normalizedValue = value.Trim();
        if (normalizedValue.Length < minimumLength)
        {
            throw new ArgumentException(
                $"The value must contain at least {minimumLength} non-whitespace characters.",
                parameterName);
        }

        return normalizedValue;
    }
}
