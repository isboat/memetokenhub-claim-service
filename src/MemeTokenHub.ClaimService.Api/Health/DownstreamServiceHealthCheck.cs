using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemeTokenHub.ClaimService.Api.Health;

public sealed class DownstreamServiceHealthCheck(IHttpClientFactory httpClientFactory, string clientName) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient(clientName);
            using HttpResponseMessage response = await client.GetAsync("health/ready", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy($"{clientName} is ready.");
            }

            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"{clientName} health endpoint returned HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"{clientName} health endpoint is unavailable.",
                exception);
        }
    }
}
