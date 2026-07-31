namespace MemeTokenHub.ClaimService.Api.Domain;

public sealed class ClaimAuditEntry
{
    public required string Action { get; init; }

    public required string ActorId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public string? ReasonCode { get; init; }

    public string? Notes { get; init; }
}
