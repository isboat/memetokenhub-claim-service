using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MemeTokenHub.ClaimService.Api.Application;

public sealed class OutboxMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public required Guid EventId { get; init; }

    public required string EventType { get; init; }

    public required string Body { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? PublishedAt { get; set; }

    public int AttemptCount { get; set; }
}
