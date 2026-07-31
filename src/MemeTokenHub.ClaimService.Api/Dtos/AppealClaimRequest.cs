using System.ComponentModel.DataAnnotations;

namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed class AppealClaimRequest
{
    [Required, StringLength(2000, MinimumLength = 10)]
    public required string Reason { get; init; }

    [Required]
    public required ProofRequest Proof { get; init; }

    public IReadOnlyList<string> Attachments { get; init; } = [];
}
