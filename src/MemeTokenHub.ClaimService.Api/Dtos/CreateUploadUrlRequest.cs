using System.ComponentModel.DataAnnotations;

namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed class CreateUploadUrlRequest
{
    [Required, StringLength(255)]
    public required string FileName { get; init; }

    [Required, StringLength(100)]
    public required string ContentType { get; init; }

    [Range(1, 10_485_760)]
    public required long SizeInBytes { get; init; }
}
