namespace MemeTokenHub.ClaimService.Api.Application;

public sealed class ClaimConcurrencyException() : InvalidOperationException("The claim changed while the request was being processed.");
