using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Application;

public interface IAttachmentRepository
{
    Task CreateAsync(ClaimAttachment attachment, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClaimAttachment>> GetByReferencesAsync(
        IReadOnlyCollection<string> objectReferences,
        CancellationToken cancellationToken);

    Task<ClaimAttachment?> GetByReferenceAsync(string objectReference, CancellationToken cancellationToken);

    Task<bool> ReplaceAsync(ClaimAttachment attachment, AttachmentScanStatus expectedStatus, CancellationToken cancellationToken);
}
