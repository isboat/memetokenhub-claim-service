namespace MemeTokenHub.ClaimService.Api.Health;

public sealed record HealthCheckEntryResponse(string Status, double DurationMilliseconds, string? Description);
