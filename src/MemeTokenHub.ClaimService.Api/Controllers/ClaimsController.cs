using System.Security.Claims;
using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.ClaimService.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MemeTokenHub.ClaimService.Api.Controllers;

/// <summary>
/// Provides claim submission, status, moderation, appeal, and attachment operations.
/// </summary>
[ApiController]
[Route("api/claims")]
[Produces("application/json")]
public sealed class ClaimsController(IClaimService claimService, IAttachmentService attachmentService) : ControllerBase
{
    /// <summary>
    /// Submits a new claim for the authenticated user.
    /// </summary>
    [HttpPost]
    [Authorize]
    [SwaggerOperation(Summary = "Submit a claim", Description = "Validates the claimant and token before storing private evidence.")]
    [ProducesResponseType<ClaimSummaryResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ClaimSummaryResponse>> SubmitAsync(SubmitClaimRequest request, CancellationToken cancellationToken)
    {
        ClaimSummaryResponse response = await claimService.SubmitAsync(GetCurrentUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetPublicStatusAsync), new { claimId = response.ClaimId }, response);
    }

    /// <summary>
    /// Gets redacted claims belonging to the authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(Summary = "Get the current user's claims")]
    public Task<PagedResponse<ClaimSummaryResponse>> GetMyClaimsAsync(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default) =>
        claimService.GetForUserAsync(GetCurrentUserId(), NormalizeLimit(limit), NormalizeOffset(offset), cancellationToken);

    /// <summary>
    /// Gets redacted claims for a user when the caller is that user or a moderator.
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize]
    [SwaggerOperation(Summary = "Get a user's claims")]
    public Task<PagedResponse<ClaimSummaryResponse>> GetUserClaimsAsync(
        string userId,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        bool isModerator = User.IsInRole("Moderator") || User.HasClaim("capability", "moderation:claims");
        if (!isModerator && !string.Equals(userId, GetCurrentUserId(), StringComparison.Ordinal))
        {
            throw new ClaimAccessDeniedException();
        }

        return claimService.GetForUserAsync(userId, NormalizeLimit(limit), NormalizeOffset(offset), cancellationToken);
    }

    /// <summary>
    /// Gets pending claims with private evidence for authorized moderators.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Policy = "ClaimModerator")]
    [SwaggerOperation(Summary = "Get the moderator review queue")]
    public Task<PagedResponse<ClaimModeratorResponse>> GetPendingAsync(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default) =>
        claimService.GetPendingAsync(NormalizeLimit(limit), NormalizeOffset(offset), cancellationToken);

    /// <summary>
    /// Gets reviewed claim history for authorized moderators.
    /// </summary>
    [HttpGet("reviewed")]
    [Authorize(Policy = "ClaimModerator")]
    [SwaggerOperation(Summary = "Get moderator claim history")]
    public Task<PagedResponse<ClaimModeratorResponse>> GetReviewedAsync(
        [FromQuery] ClaimStatus? status,
        [FromQuery] string? reviewerId,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default) =>
        claimService.GetReviewedAsync(status, reviewerId, NormalizeLimit(limit), NormalizeOffset(offset), cancellationToken);

    /// <summary>
    /// Gets a public, evidence-free claim status suitable for verification badges.
    /// </summary>
    [HttpGet("{claimId}/public-status")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get public claim status")]
    public Task<PublicClaimStatusResponse> GetPublicStatusAsync(string claimId, CancellationToken cancellationToken) =>
        claimService.GetPublicStatusAsync(claimId, cancellationToken);

    /// <summary>
    /// Approves or rejects a pending claim as an authorized moderator.
    /// </summary>
    [HttpPut("{claimId}/review")]
    [Authorize(Policy = "ClaimModerator")]
    [SwaggerOperation(Summary = "Review a pending claim")]
    public Task<ClaimSummaryResponse> ReviewAsync(
        string claimId,
        ReviewClaimRequest request,
        CancellationToken cancellationToken)
    {
        string correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        return claimService.ReviewAsync(claimId, GetCurrentUserId(), request, correlationId, cancellationToken);
    }

    /// <summary>
    /// Submits the claim owner's single appeal for a rejected claim.
    /// </summary>
    [HttpPost("{claimId}/appeal")]
    [Authorize]
    [SwaggerOperation(Summary = "Appeal a rejected claim")]
    public Task<ClaimSummaryResponse> AppealAsync(
        string claimId,
        AppealClaimRequest request,
        CancellationToken cancellationToken) =>
        claimService.AppealAsync(claimId, GetCurrentUserId(), request, cancellationToken);

    /// <summary>
    /// Creates a short-lived signed URL for an approved evidence file type.
    /// </summary>
    [HttpPost("attachments/upload-url")]
    [Authorize]
    [SwaggerOperation(Summary = "Create an evidence upload URL")]
    public Task<UploadUrlResponse> CreateUploadUrlAsync(
        CreateUploadUrlRequest request,
        CancellationToken cancellationToken) =>
        attachmentService.CreateUploadUrlAsync(GetCurrentUserId(), request, cancellationToken);

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new ClaimAccessDeniedException();

    private static int NormalizeLimit(int limit) => Math.Clamp(limit, 1, 100);

    private static int NormalizeOffset(int offset) => Math.Max(offset, 0);
}
