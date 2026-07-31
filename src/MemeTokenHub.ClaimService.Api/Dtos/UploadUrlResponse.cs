namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed record UploadUrlResponse(Uri UploadUrl, string ObjectReference, DateTimeOffset ExpiresAt);
