namespace MemeTokenHub.ClaimService.Api.Domain;

public sealed class InvalidClaimTransitionException(string message) : InvalidOperationException(message);
