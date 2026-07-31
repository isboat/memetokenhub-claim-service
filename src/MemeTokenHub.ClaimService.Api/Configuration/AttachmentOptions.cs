using System.ComponentModel.DataAnnotations;

namespace MemeTokenHub.ClaimService.Api.Configuration;

public sealed class AttachmentOptions
{
    public const string SectionName = "Attachments";

    [Required, Url]
    public required string UploadBaseUrl { get; init; }

    [Required, MinLength(32)]
    public required string SigningKey { get; init; }

    public int UploadLifetimeMinutes { get; init; } = 10;

    public long MaximumSizeInBytes { get; init; } = 10_485_760;

    public string[] AllowedContentTypes { get; init; } = ["application/pdf", "image/jpeg", "image/png"];
}
