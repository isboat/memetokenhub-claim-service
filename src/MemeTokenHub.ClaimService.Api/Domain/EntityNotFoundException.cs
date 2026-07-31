namespace MemeTokenHub.ClaimService.Api.Domain;

public sealed class EntityNotFoundException(string message) : InvalidOperationException(message);
