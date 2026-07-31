using System.Net;
using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Domain;

namespace MemeTokenHub.ClaimService.Api.Infrastructure;

public sealed partial class ReferenceValidationService(
    IHttpClientFactory httpClientFactory,
    ILogger<ReferenceValidationService> logger) : IReferenceValidationService
{
    public Task ValidateClaimantAsync(string userId, CancellationToken cancellationToken) =>
        ValidateAsync("UserService", $"internal/users/{Uri.EscapeDataString(userId)}/exists", "claimant", cancellationToken);

    public Task ValidateTokenAsync(string tokenId, CancellationToken cancellationToken) =>
        ValidateAsync("TokenService", $"internal/tokens/{Uri.EscapeDataString(tokenId)}/exists", "token", cancellationToken);

    private async Task ValidateAsync(string clientName, string path, string resourceName, CancellationToken cancellationToken)
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient(clientName);
            using HttpResponseMessage response = await client.GetAsync(path, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new EntityNotFoundException($"The referenced {resourceName} does not exist.");
            }

            response.EnsureSuccessStatusCode();
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            LogValidationUnavailable(logger, exception, resourceName);
            throw new DependencyUnavailableException($"The {resourceName} could not be validated.", exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to validate the referenced {ResourceName}.")]
    private static partial void LogValidationUnavailable(ILogger logger, Exception exception, string resourceName);
}
