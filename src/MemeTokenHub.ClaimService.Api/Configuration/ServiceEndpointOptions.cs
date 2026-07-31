using System.ComponentModel.DataAnnotations;

namespace MemeTokenHub.ClaimService.Api.Configuration;

public sealed class ServiceEndpointOptions
{
    public const string SectionName = "ServiceEndpoints";

    [Required, Url]
    public required string UserService { get; init; }

    [Required, Url]
    public required string TokenService { get; init; }
}
