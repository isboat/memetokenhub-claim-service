using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MemeTokenHub.ClaimService.Api.Controllers;

/// <summary>
/// Receives trusted evidence scanning results from the attachment scanning worker.
/// </summary>
[ApiController]
[Route("api/internal/claim-attachments")]
[Authorize(Policy = "AttachmentScanner")]
public sealed class AttachmentScanController(IAttachmentService attachmentService) : ControllerBase
{
    /// <summary>
    /// Records the final malware, content-type, and size scanning decision for an uploaded evidence object.
    /// </summary>
    [HttpPost("scan-result")]
    [SwaggerOperation(Summary = "Record an attachment scan result")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordScanResultAsync(
        RecordAttachmentScanResultRequest request,
        CancellationToken cancellationToken)
    {
        await attachmentService.RecordScanResultAsync(request, cancellationToken);
        return NoContent();
    }
}
