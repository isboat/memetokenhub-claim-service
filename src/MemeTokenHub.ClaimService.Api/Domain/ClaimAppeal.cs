namespace MemeTokenHub.ClaimService.Api.Domain;

public sealed class ClaimAppeal
{
    public required string Reason { get; init; }

    public required ProofSnapshot Proof { get; init; }

    public IReadOnlyList<string> Attachments { get; init; } = [];

    public required DateTimeOffset SubmittedAt { get; init; }
}
