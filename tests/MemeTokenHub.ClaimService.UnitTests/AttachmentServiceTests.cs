using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Configuration;
using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.ClaimService.Api.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;

namespace MemeTokenHub.ClaimService.UnitTests;

public sealed class AttachmentServiceTests
{
    [Test]
    public void ValidateReferencesAsyncRejectsAttachmentOwnedByAnotherUser()
    {
        ClaimAttachment attachment = CreateAttachment("user-2");
        attachment.RecordScanResult(true, null, DateTimeOffset.UtcNow);
        Mock<IAttachmentRepository> repository = CreateRepository(attachment);
        AttachmentService service = CreateService(repository.Object);

        AsyncTestDelegate validation = async () =>
            await service.ValidateReferencesAsync("user-1", [attachment.ObjectReference], CancellationToken.None);

        Assert.That(validation, Throws.TypeOf<InvalidClaimAttachmentException>());
    }

    [Test]
    public void ValidateReferencesAsyncRejectsPendingAttachment()
    {
        ClaimAttachment attachment = CreateAttachment("user-1");
        Mock<IAttachmentRepository> repository = CreateRepository(attachment);
        AttachmentService service = CreateService(repository.Object);

        AsyncTestDelegate validation = async () =>
            await service.ValidateReferencesAsync("user-1", [attachment.ObjectReference], CancellationToken.None);

        Assert.That(validation, Throws.TypeOf<InvalidClaimAttachmentException>());
    }

    [Test]
    public void ValidateReferencesAsyncAcceptsOwnedApprovedAttachment()
    {
        ClaimAttachment attachment = CreateAttachment("user-1");
        attachment.RecordScanResult(true, null, DateTimeOffset.UtcNow);
        Mock<IAttachmentRepository> repository = CreateRepository(attachment);
        AttachmentService service = CreateService(repository.Object);

        AsyncTestDelegate validation = async () =>
            await service.ValidateReferencesAsync("user-1", [attachment.ObjectReference], CancellationToken.None);

        Assert.That(validation, Throws.Nothing);
    }

    private static Mock<IAttachmentRepository> CreateRepository(ClaimAttachment attachment)
    {
        Mock<IAttachmentRepository> repository = new();
        repository.Setup(item => item.GetByReferencesAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([attachment]);
        return repository;
    }

    private static AttachmentService CreateService(IAttachmentRepository repository)
    {
        AttachmentOptions options = new()
        {
            UploadBaseUrl = "https://storage.example.test/upload",
            SigningKey = "a-signing-key-containing-at-least-32-characters"
        };
        return new AttachmentService(Options.Create(options), repository, TimeProvider.System);
    }

    private static ClaimAttachment CreateAttachment(string userId) => new()
    {
        ObjectReference = $"claims/{userId}/evidence.pdf",
        UserId = userId,
        FileName = "evidence.pdf",
        DeclaredContentType = "application/pdf",
        DeclaredSizeInBytes = 1024,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
