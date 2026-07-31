using System.Net;
using System.Text.Json;
using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.ClaimService.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MemeTokenHub.ClaimService.IntegrationTests;

public sealed class ClaimApiEndpointTests
{
    [Test]
    public void JsonConfigurationRejectsNumericEnumValues()
    {
        using ClaimApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        JsonOptions jsonOptions = scope.ServiceProvider.GetRequiredService<IOptions<JsonOptions>>().Value;
        const string request = """
            {
              "tokenId": "token-1",
              "type": 99,
              "description": "This description is long enough.",
              "proof": { "method": "walletSignature" }
            }
            """;

        TestDelegate deserialize = () => JsonSerializer.Deserialize<SubmitClaimRequest>(request, jsonOptions.JsonSerializerOptions);

        Assert.That(deserialize, Throws.TypeOf<JsonException>());
    }

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
