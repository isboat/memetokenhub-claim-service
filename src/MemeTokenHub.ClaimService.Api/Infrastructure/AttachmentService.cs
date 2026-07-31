using System.Security.Cryptography;
using System.Text;
using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Configuration;
using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.ClaimService.Api.Dtos;
using Microsoft.Extensions.Options;

namespace MemeTokenHub.ClaimService.Api.Infrastructure;

public sealed class AttachmentService(
    IOptions<AttachmentOptions> options,
    IAttachmentRepository attachmentRepository,
    TimeProvider timeProvider) : IAttachmentService
{
    private readonly AttachmentOptions _options = options.Value;

    public async Task<UploadUrlResponse> CreateUploadUrlAsync(
        string userId,
        CreateUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (!_options.AllowedContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The attachment content type is not allowed.", nameof(request));
        }

        if (request.SizeInBytes > _options.MaximumSizeInBytes)
        {
            throw new ArgumentException("The attachment is larger than the configured maximum.", nameof(request));
        }

        DateTimeOffset expiresAt = timeProvider.GetUtcNow().AddMinutes(_options.UploadLifetimeMinutes);
        string safeExtension = Path.GetExtension(request.FileName).ToLowerInvariant();
        string objectReference = $"claims/{userId}/{Guid.NewGuid():N}{safeExtension}";
        string valueToSign = $"{objectReference}|{expiresAt.ToUnixTimeSeconds()}|{request.ContentType}|{request.SizeInBytes}";
        byte[] key = Encoding.UTF8.GetBytes(_options.SigningKey);
        string signature = Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(valueToSign)));
        string uploadUrl = $"{_options.UploadBaseUrl.TrimEnd('/')}/{objectReference}"
            + $"?expires={expiresAt.ToUnixTimeSeconds()}&contentType={Uri.EscapeDataString(request.ContentType)}"
            + $"&maxSize={request.SizeInBytes}&signature={signature}";
        ClaimAttachment attachment = new()
        {
            ObjectReference = objectReference,
            UserId = userId,
            FileName = request.FileName,
            DeclaredContentType = request.ContentType,
            DeclaredSizeInBytes = request.SizeInBytes,
            CreatedAt = timeProvider.GetUtcNow()
        };
        await attachmentRepository.CreateAsync(attachment, cancellationToken);
        return new UploadUrlResponse(new Uri(uploadUrl), objectReference, expiresAt);
    }

    public async Task ValidateReferencesAsync(
        string userId,
        IReadOnlyList<string> objectReferences,
        CancellationToken cancellationToken)
    {
        if (objectReferences.Count > 10 || objectReferences.Count != objectReferences.Distinct(StringComparer.Ordinal).Count())
        {
            throw new InvalidClaimAttachmentException("Attachments must contain no more than ten unique object references.");
        }

        if (objectReferences.Count == 0)
        {
            return;
        }

        IReadOnlyList<ClaimAttachment> attachments = await attachmentRepository.GetByReferencesAsync(objectReferences, cancellationToken);
        bool referencesAreApprovedAndOwned = attachments.Count == objectReferences.Count
            && attachments.All(attachment =>
                string.Equals(attachment.UserId, userId, StringComparison.Ordinal)
                && attachment.ScanStatus == AttachmentScanStatus.Approved);
        if (!referencesAreApprovedAndOwned)
        {
            throw new InvalidClaimAttachmentException(
                "Every attachment must belong to the claimant and have a completed, approved security scan.");
        }
    }

    public async Task RecordScanResultAsync(RecordAttachmentScanResultRequest request, CancellationToken cancellationToken)
    {
        ClaimAttachment attachment = await attachmentRepository.GetByReferenceAsync(request.ObjectReference, cancellationToken)
            ?? throw new EntityNotFoundException("The attachment upload was not found.");
        AttachmentScanStatus previousStatus = attachment.ScanStatus;
        attachment.RecordScanResult(request.Approved, request.RejectionReason?.Trim(), timeProvider.GetUtcNow());
        if (!await attachmentRepository.ReplaceAsync(attachment, previousStatus, cancellationToken))
        {
            throw new ClaimConcurrencyException();
        }
    }
}
