namespace MemeTokenHub.ClaimService.Api.Application;

public sealed class ClaimAccessDeniedException() : UnauthorizedAccessException("The current user does not own this claim.");
