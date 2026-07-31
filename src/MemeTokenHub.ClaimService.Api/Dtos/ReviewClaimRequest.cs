using System.ComponentModel.DataAnnotations;
using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed class ReviewClaimRequest
{
    public required ClaimStatus Status { get; init; }

    [Required, StringLength(2000)]
    public required string Notes { get; init; }

    [Required, StringLength(100)]
    public required string ReasonCode { get; init; }

    public required long ExpectedVersion { get; init; }
}
