using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MemeTokenHub.ClaimService.Api.Domain;

public sealed class Claim
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; init; } = ObjectId.GenerateNewId().ToString();

    public required string ClaimId { get; init; }

    public required string UserId { get; init; }

    public required string TokenId { get; init; }

    public required ClaimType Type { get; init; }

    public required string Description { get; init; }

    public IReadOnlyList<string> Attachments { get; init; } = [];

    public required ProofSnapshot Proof { get; init; }

    public ClaimStatus Status { get; private set; } = ClaimStatus.Pending;

    public string? ReviewerId { get; private set; }

    public string? ReviewNotes { get; private set; }

    public string? ModerationReasonCode { get; private set; }

    public required DateTimeOffset SubmittedAt { get; init; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public ClaimAppeal? Appeal { get; private set; }

    public long Version { get; private set; }

    public List<ClaimAuditEntry> AuditHistory { get; init; } = [];

    public void Review(ClaimStatus decision, string reviewerId, string notes, string reasonCode, DateTimeOffset reviewedAt)
    {
        if (!Enum.IsDefined(decision))
        {
            throw new InvalidClaimTransitionException("The review decision is not a defined claim status.");
        }

        if (Status != ClaimStatus.Pending)
        {
            throw new InvalidClaimTransitionException("Only pending claims can be reviewed.");
        }

        if (decision == ClaimStatus.Pending)
        {
            throw new InvalidClaimTransitionException("A review decision must approve or reject the claim.");
        }

        Status = decision;
        ReviewerId = reviewerId;
        ReviewNotes = notes;
        ModerationReasonCode = reasonCode;
        ReviewedAt = reviewedAt;
        Version++;
        AuditHistory.Add(new ClaimAuditEntry
        {
            Action = decision.ToString(),
            ActorId = reviewerId,
            OccurredAt = reviewedAt,
            ReasonCode = reasonCode,
            Notes = notes
        });
    }

    public void SubmitAppeal(ClaimAppeal appeal, string actorId)
    {
        if (Status != ClaimStatus.Rejected)
        {
            throw new InvalidClaimTransitionException("Only rejected claims can be appealed.");
        }

        if (Appeal is not null)
        {
            throw new InvalidClaimTransitionException("A claim can only be appealed once.");
        }

        Appeal = appeal;
        Status = ClaimStatus.Pending;
        ReviewerId = null;
        ReviewNotes = null;
        ReviewedAt = null;
        Version++;
        AuditHistory.Add(new ClaimAuditEntry
        {
            Action = "Appealed",
            ActorId = actorId,
            OccurredAt = appeal.SubmittedAt
        });
    }
}
