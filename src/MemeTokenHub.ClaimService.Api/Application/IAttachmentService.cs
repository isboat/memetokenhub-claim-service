using MemeTokenHub.ClaimService.Api.Dtos;

namespace MemeTokenHub.ClaimService.Api.Application;

public interface IAttachmentService
{
    Task<UploadUrlResponse> CreateUploadUrlAsync(string userId, CreateUploadUrlRequest request, CancellationToken cancellationToken);

    Task ValidateReferencesAsync(string userId, IReadOnlyList<string> objectReferences, CancellationToken cancellationToken);

    Task RecordScanResultAsync(RecordAttachmentScanResultRequest request, CancellationToken cancellationToken);
}
