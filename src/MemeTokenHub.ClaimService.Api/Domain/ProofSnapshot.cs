namespace MemeTokenHub.ClaimService.Api.Domain;

public sealed class ProofSnapshot
{
    public required ProofMethod Method { get; init; }

    public IReadOnlyList<string> SocialLinks { get; init; } = [];

    public string? WalletTransaction { get; init; }

    public string? VerificationValue { get; init; }
}
