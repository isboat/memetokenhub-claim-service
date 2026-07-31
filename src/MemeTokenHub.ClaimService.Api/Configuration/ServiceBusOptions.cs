using System.ComponentModel.DataAnnotations;

namespace MemeTokenHub.ClaimService.Api.Configuration;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public bool Enabled { get; init; } = true;

    [Required]
    public required string ConnectionString { get; init; }

    [Required]
    public required string TopicName { get; init; }
}
