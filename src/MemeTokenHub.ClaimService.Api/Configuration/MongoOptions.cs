using System.ComponentModel.DataAnnotations;

namespace MemeTokenHub.ClaimService.Api.Configuration;

public sealed class MongoOptions
{
    public const string SectionName = "MongoDb";

    [Required]
    public required string ConnectionString { get; init; }

    [Required]
    public required string DatabaseName { get; init; }
}
