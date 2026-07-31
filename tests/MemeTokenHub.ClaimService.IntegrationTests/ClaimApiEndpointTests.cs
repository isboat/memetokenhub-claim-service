using System.Net;
using System.Text.Json;
using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.ClaimService.Api.Dtos;

namespace MemeTokenHub.ClaimService.IntegrationTests;

public sealed class ClaimApiEndpointTests
{
    [Test]
    public async Task GetLivenessReturnsHealthyStatus()
    {
        await using ClaimApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/live");
        string responseBody = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseBody, Is.EqualTo("Healthy"));
        });
    }

    [Test]
    public async Task GetPublicStatusWithoutAuthenticationReturnsRedactedApprovedStatus()
    {
        await using ClaimApiFactory factory = new();
        using HttpClient client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client.GetAsync("/api/claims/claim-1/public-status");
        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument body = JsonDocument.Parse(json);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body.RootElement.GetProperty("status").GetString(), Is.EqualTo("approved"));
            Assert.That(body.RootElement.GetProperty("claimId").GetString(), Is.EqualTo("claim-1"));
            Assert.That(json, Does.Not.Contain("proof"));
            Assert.That(json, Does.Not.Contain("reviewNotes"));
        });
    }
}
