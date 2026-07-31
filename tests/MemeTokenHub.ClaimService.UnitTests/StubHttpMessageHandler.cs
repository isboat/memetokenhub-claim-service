using System.Net;

namespace MemeTokenHub.ClaimService.UnitTests;

internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(statusCode));
}
