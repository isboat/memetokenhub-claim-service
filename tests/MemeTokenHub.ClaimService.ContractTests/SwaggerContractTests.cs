using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MemeTokenHub.ClaimService.ContractTests;

public sealed class SwaggerContractTests
{
    [Test]
    public async Task SwaggerDocumentDescribesAllRequiredClaimRoutes()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement paths = document.RootElement.GetProperty("paths");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(paths.TryGetProperty("/api/claims", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/claims/{claimId}/review", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/claims/{claimId}/appeal", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/claims/{claimId}/public-status", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/claims/attachments/upload-url", out _), Is.True);
        });
    }
}
