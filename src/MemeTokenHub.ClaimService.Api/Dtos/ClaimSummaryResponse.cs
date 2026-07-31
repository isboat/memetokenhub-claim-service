using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed record ClaimSummaryResponse(
    string ClaimId,
    string UserId,
    string TokenId,
    ClaimType Type,
    ClaimStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    long Version);
