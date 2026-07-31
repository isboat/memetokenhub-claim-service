using System.ComponentModel.DataAnnotations;

namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed class RecordAttachmentScanResultRequest : IValidatableObject
{
    [Required]
    public required string ObjectReference { get; init; }

    public required bool Approved { get; init; }

    [StringLength(500)]
    public string? RejectionReason { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Approved && string.IsNullOrWhiteSpace(RejectionReason))
        {
            yield return new ValidationResult(
                "A rejection reason is required when an attachment is rejected.",
                [nameof(RejectionReason)]);
        }
    }
}
