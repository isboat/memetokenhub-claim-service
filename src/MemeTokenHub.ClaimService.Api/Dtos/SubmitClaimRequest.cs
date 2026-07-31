using System.ComponentModel.DataAnnotations;
using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed class SubmitClaimRequest
{
    [Required, StringLength(100)]
    public required string TokenId { get; init; }

    public required ClaimType Type { get; init; }

    [Required, StringLength(2000, MinimumLength = 10)]
    public required string Description { get; init; }

    public IReadOnlyList<string> Attachments { get; init; } = [];

    [Required]
    public required ProofRequest Proof { get; init; }
}
