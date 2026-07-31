using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.ClaimService.Api.Dtos;
using Moq;

namespace MemeTokenHub.ClaimService.UnitTests;

using ApplicationClaimService = MemeTokenHub.ClaimService.Api.Application.ClaimService;

public sealed class ClaimServiceTests
{
    [Test]
    public void SubmitAsyncRejectsWhitespaceOnlyDescriptionAfterNormalization()
    {
        Mock<IReferenceValidationService> referenceValidation = new();
        ApplicationClaimService service = new(
            Mock.Of<IClaimRepository>(),
            referenceValidation.Object,
            Mock.Of<IAttachmentService>(),
            Mock.Of<IEventEnvelopeFactory>(),
            TimeProvider.System);
        SubmitClaimRequest request = new()
        {
            TokenId = "token-1",
            Type = ClaimType.ProjectOwnership,
            Description = "          ",
            Proof = new ProofRequest { Method = ProofMethod.WalletSignature }
        };

        AsyncTestDelegate submit = async () => await service.SubmitAsync("user-1", request, CancellationToken.None);

        Assert.That(submit, Throws.TypeOf<ArgumentException>());
        referenceValidation.Verify(
            item => item.ValidateClaimantAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReviewAsyncWithApprovalPersistsClaimAndOutboxTogether()
    {
        Claim claim = CreateClaim();
        Mock<IClaimRepository> repository = new();
        repository.Setup(item => item.GetByClaimIdAsync(claim.ClaimId, It.IsAny<CancellationToken>())).ReturnsAsync(claim);
        repository.Setup(item => item.ReplaceWithApprovalOutboxAsync(claim, 0, It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IEventEnvelopeFactory> envelopeFactory = new();
        envelopeFactory.Setup(item => item.CreateClaimApproved(claim, "correlation-1")).Returns(new OutboxMessage
        {
            EventId = Guid.NewGuid(),
            EventType = "ClaimApproved",
            Body = "{}",
            OccurredAt = DateTimeOffset.UtcNow
        });
        ApplicationClaimService service = CreateService(repository.Object, envelopeFactory.Object);
        ReviewClaimRequest request = new()
        {
            Status = ClaimStatus.Approved,
            Notes = "Verified.",
            ReasonCode = "VALID_PROOF",
            ExpectedVersion = 0
        };

        ClaimSummaryResponse result = await service.ReviewAsync(claim.ClaimId, "moderator-1", request, "correlation-1", CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ClaimStatus.Approved));
        repository.Verify(item => item.ReplaceWithApprovalOutboxAsync(claim, 0, It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(item => item.ReplaceAsync(It.IsAny<Claim>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetPublicStatusAsyncDoesNotReturnEvidenceOrReviewNotes()
    {
        Claim claim = CreateClaim();
        claim.Review(ClaimStatus.Approved, "moderator-1", "Private review notes", "VALID_PROOF", DateTimeOffset.UtcNow);
        Mock<IClaimRepository> repository = new();
        repository.Setup(item => item.GetByClaimIdAsync(claim.ClaimId, It.IsAny<CancellationToken>())).ReturnsAsync(claim);
        ApplicationClaimService service = CreateService(repository.Object, Mock.Of<IEventEnvelopeFactory>());

        PublicClaimStatusResponse response = await service.GetPublicStatusAsync(claim.ClaimId, CancellationToken.None);
        string serialized = System.Text.Json.JsonSerializer.Serialize(response);

        Assert.That(serialized, Does.Not.Contain("Private review notes"));
        Assert.That(serialized, Does.Not.Contain("WalletSignature"));
    }

    private static ApplicationClaimService CreateService(IClaimRepository repository, IEventEnvelopeFactory envelopeFactory) => new(
        repository,
        Mock.Of<IReferenceValidationService>(),
        Mock.Of<IAttachmentService>(),
        envelopeFactory,
        TimeProvider.System);

    private static Claim CreateClaim() => new()
    {
        ClaimId = "claim-1",
        UserId = "user-1",
        TokenId = "token-1",
        Type = ClaimType.ProjectOwnership,
        Description = "I own this token project.",
        Proof = new ProofSnapshot { Method = ProofMethod.WalletSignature, VerificationValue = "private-proof" },
        SubmittedAt = DateTimeOffset.UtcNow
    };
}
