using System.ComponentModel.DataAnnotations;
using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed class ProofRequest
{
    public required ProofMethod Method { get; init; }

    public IReadOnlyList<Uri> SocialLinks { get; init; } = [];

    [StringLength(256)]
    public string? WalletTransaction { get; init; }

    [StringLength(2048)]
    public string? VerificationValue { get; init; }
}
