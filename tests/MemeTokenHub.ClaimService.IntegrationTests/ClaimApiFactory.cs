using MemeTokenHub.ClaimService.Api.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MemeTokenHub.ClaimService.IntegrationTests;

internal sealed class ClaimApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IClaimService>();
            services.AddSingleton<IClaimService, TestClaimService>();
        });
    }
}
