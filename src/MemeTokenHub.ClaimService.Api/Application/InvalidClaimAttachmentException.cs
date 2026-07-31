namespace MemeTokenHub.ClaimService.Api.Application;

public sealed class InvalidClaimAttachmentException(string message) : InvalidOperationException(message);
