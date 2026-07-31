using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed record ClaimModeratorResponse(
    string ClaimId,
    string UserId,
    string TokenId,
    ClaimType Type,
    string Description,
    IReadOnlyList<string> Attachments,
    ProofSnapshot Proof,
    ClaimStatus Status,
    string? ReviewerId,
    string? ReviewNotes,
    string? ModerationReasonCode,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    ClaimAppeal? Appeal,
    IReadOnlyList<ClaimAuditEntry> AuditHistory,
    long Version);
