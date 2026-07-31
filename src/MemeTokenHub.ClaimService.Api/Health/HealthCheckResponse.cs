namespace MemeTokenHub.ClaimService.Api.Health;

public sealed record HealthCheckResponse(
    string Status,
    double TotalDurationMilliseconds,
    DateTimeOffset CheckedAt,
    IReadOnlyDictionary<string, HealthCheckEntryResponse> Checks);
