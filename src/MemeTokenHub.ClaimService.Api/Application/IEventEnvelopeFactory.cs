using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Application;

public interface IEventEnvelopeFactory
{
    OutboxMessage CreateClaimApproved(Claim claim, string correlationId);
}
