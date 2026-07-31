using System.Security.Cryptography;
using System.Text;
using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Configuration;
using MemeTokenHub.ClaimService.Api.Dtos;
using Microsoft.Extensions.Options;

namespace MemeTokenHub.ClaimService.Api.Infrastructure;

public sealed class AttachmentService(IOptions<AttachmentOptions> options, TimeProvider timeProvider) : IAttachmentService
{
    private readonly AttachmentOptions _options = options.Value;

    public Task<UploadUrlResponse> CreateUploadUrlAsync(
        string userId,
        CreateUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
        string valueToSign = $"{objectReference}|{expiresAt.ToUnixTimeSeconds()}|{request.ContentType}";
        byte[] key = Encoding.UTF8.GetBytes(_options.SigningKey);
        string signature = Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(valueToSign)));
        string uploadUrl = $"{_options.UploadBaseUrl.TrimEnd('/')}/{objectReference}?expires={expiresAt.ToUnixTimeSeconds()}&signature={signature}";
        return Task.FromResult(new UploadUrlResponse(new Uri(uploadUrl), objectReference, expiresAt));
    }

    public Task ValidateReferencesAsync(IReadOnlyList<string> objectReferences, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (objectReferences.Count > 10 || objectReferences.Any(reference => !reference.StartsWith("claims/", StringComparison.Ordinal)))
        {
            throw new ArgumentException("Attachments must contain no more than ten approved claim object references.", nameof(objectReferences));
        }

        return Task.CompletedTask;
    }
}
