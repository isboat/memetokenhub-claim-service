namespace MemeTokenHub.ClaimService.Api.Domain;

public sealed class DependencyUnavailableException(string message, Exception? innerException = null) : Exception(message, innerException);
