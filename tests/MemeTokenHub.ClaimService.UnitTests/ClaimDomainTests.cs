using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.UnitTests;

public sealed class ClaimDomainTests
{
    [Test]
    public void ReviewWithApprovedDecisionRecordsAuditAndIncrementsVersion()
    {
        Claim claim = CreateClaim();

        claim.Review(ClaimStatus.Approved, "moderator-1", "Evidence verified.", "VALID_PROOF", DateTimeOffset.Parse("2026-07-31T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

        Assert.Multiple(() =>
        {
            Assert.That(claim.Status, Is.EqualTo(ClaimStatus.Approved));
            Assert.That(claim.Version, Is.EqualTo(1));
            Assert.That(claim.AuditHistory, Has.Count.EqualTo(1));
            Assert.That(claim.ReviewerId, Is.EqualTo("moderator-1"));
        });
    }

    [Test]
    public void SubmitAppealAfterPriorAppealThrowsInvalidTransition()
    {
        Claim claim = CreateClaim();
        claim.Review(ClaimStatus.Rejected, "moderator-1", "Insufficient proof.", "PROOF_INVALID", DateTimeOffset.UtcNow);
        ClaimAppeal appeal = new()
        {
            Reason = "Additional proof is now available.",
            Proof = claim.Proof,
            SubmittedAt = DateTimeOffset.UtcNow
        };
        claim.SubmitAppeal(appeal, claim.UserId);
        ClaimAuditEntry originalReview = claim.AuditHistory.Single(entry => entry.Action == ClaimStatus.Rejected.ToString());
        claim.Review(ClaimStatus.Rejected, "moderator-2", "Proof is still invalid.", "PROOF_INVALID", DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(originalReview.Notes, Is.EqualTo("Insufficient proof."));
            Assert.That(() => claim.SubmitAppeal(appeal, claim.UserId), Throws.TypeOf<InvalidClaimTransitionException>());
        });
    }

    [Test]
    public void ReviewWithUndefinedStatusThrowsInvalidTransition()
    {
        Claim claim = CreateClaim();

        TestDelegate review = () => claim.Review(
            (ClaimStatus)99,
            "moderator-1",
            "Invalid status must not be persisted.",
            "INVALID_STATUS",
            DateTimeOffset.UtcNow);

        Assert.That(review, Throws.TypeOf<InvalidClaimTransitionException>());
    }

    private static Claim CreateClaim() => new()
    {
        ClaimId = "claim-1",
        UserId = "user-1",
        TokenId = "token-1",
        Type = ClaimType.ProjectOwnership,
        Description = "I own this token project.",
        Proof = new ProofSnapshot { Method = ProofMethod.WalletSignature },
        SubmittedAt = DateTimeOffset.UtcNow
    };
}
