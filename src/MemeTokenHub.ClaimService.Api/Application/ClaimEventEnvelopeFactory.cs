using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.Shared.Messaging;

namespace MemeTokenHub.ClaimService.Api.Application;

public sealed class ClaimEventEnvelopeFactory(TimeProvider timeProvider) : IEventEnvelopeFactory
{
    public OutboxMessage CreateClaimApproved(Claim claim, string correlationId)
    {
        ClaimApprovedEvent payload = new(claim.ClaimId, claim.UserId, claim.TokenId, claim.Status.ToString());
        EventEnvelope<ClaimApprovedEvent> envelope = EventEnvelope<ClaimApprovedEvent>.Create(
            payload,
            "claim-service",
            claim.ClaimId,
            correlationId,
            timeProvider: timeProvider);

        return new OutboxMessage
        {
            EventId = envelope.EventId,
            EventType = envelope.EventType,
            Body = EventSerializer.Serialize(envelope),
            OccurredAt = envelope.OccurredAt
        };
    }
}
