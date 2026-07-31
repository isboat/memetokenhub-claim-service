using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MemeTokenHub.ClaimService.Api.Domain;

public sealed class ClaimAttachment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; init; } = ObjectId.GenerateNewId().ToString();

    public required string ObjectReference { get; init; }

    public required string UserId { get; init; }

    public required string FileName { get; init; }

    public required string DeclaredContentType { get; init; }

    public required long DeclaredSizeInBytes { get; init; }

    public AttachmentScanStatus ScanStatus { get; private set; } = AttachmentScanStatus.Pending;

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ScannedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public void RecordScanResult(bool approved, string? rejectionReason, DateTimeOffset scannedAt)
    {
        if (ScanStatus != AttachmentScanStatus.Pending)
        {
            throw new InvalidOperationException("An attachment scan result can only be recorded once.");
        }

        ScanStatus = approved ? AttachmentScanStatus.Approved : AttachmentScanStatus.Rejected;
        RejectionReason = approved ? null : rejectionReason;
        ScannedAt = scannedAt;
    }
}
