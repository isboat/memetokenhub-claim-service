using System.Net.Http.Headers;

namespace MemeTokenHub.ClaimService.Api.Infrastructure;

public sealed class ServiceAuthenticationHandler(IConfiguration configuration) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? token = configuration["ServiceAuthentication:BearerToken"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
