namespace MemeTokenHub.ClaimService.Api.Application;

public interface IReferenceValidationService
{
    Task ValidateClaimantAsync(string userId, CancellationToken cancellationToken);

    Task ValidateTokenAsync(string tokenId, CancellationToken cancellationToken);
}
