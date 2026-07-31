using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Dtos;

public static class ClaimMapper
{
    public static ClaimSummaryResponse ToSummary(this Claim claim) => new(
        claim.ClaimId,
        claim.UserId,
        claim.TokenId,
        claim.Type,
        claim.Status,
        claim.SubmittedAt,
        claim.ReviewedAt,
        claim.Version);

    public static PublicClaimStatusResponse ToPublicStatus(this Claim claim) => new(
        claim.ClaimId,
        claim.UserId,
        claim.TokenId,
        claim.Type,
        claim.Status,
        claim.SubmittedAt,
        claim.Status == ClaimStatus.Approved ? claim.ReviewedAt : null);

    public static ClaimModeratorResponse ToModeratorResponse(this Claim claim) => new(
        claim.ClaimId,
        claim.UserId,
        claim.TokenId,
        claim.Type,
        claim.Description,
        claim.Attachments,
        claim.Proof,
        claim.Status,
        claim.ReviewerId,
        claim.ReviewNotes,
        claim.ModerationReasonCode,
        claim.SubmittedAt,
        claim.ReviewedAt,
        claim.Appeal,
        claim.AuditHistory,
        claim.Version);

    public static ProofSnapshot ToSnapshot(this ProofRequest proof) => new()
    {
        Method = proof.Method,
        SocialLinks = proof.SocialLinks.Select(link => link.ToString()).ToArray(),
        WalletTransaction = proof.WalletTransaction,
        VerificationValue = proof.VerificationValue
    };
}
