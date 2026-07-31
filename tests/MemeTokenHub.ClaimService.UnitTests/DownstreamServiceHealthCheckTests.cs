using System.Net;
using MemeTokenHub.ClaimService.Api.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemeTokenHub.ClaimService.UnitTests;

public sealed class DownstreamServiceHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsyncReturnsHealthyForSuccessfulReadyEndpoint()
    {
        using HttpClient client = new(new StubHttpMessageHandler(HttpStatusCode.OK))
        {
            BaseAddress = new Uri("https://user-service.example.test/")
        };
        DownstreamServiceHealthCheck healthCheck = new(new StubHttpClientFactory(client), "UserService");

        HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task CheckHealthAsyncReturnsUnhealthyForFailedReadyEndpoint()
    {
        using HttpClient client = new(new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable))
        {
            BaseAddress = new Uri("https://token-service.example.test/")
        };
        DownstreamServiceHealthCheck healthCheck = new(new StubHttpClientFactory(client), "TokenService");

        HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
    }

    private static HealthCheckContext CreateContext() => new()
    {
        Registration = new HealthCheckRegistration(
            "downstream-service",
            _ => throw new InvalidOperationException("The factory is not used by this unit test."),
            HealthStatus.Unhealthy,
            null)
    };
}
