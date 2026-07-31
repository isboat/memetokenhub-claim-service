using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.ClaimService.Api.Dtos;

namespace MemeTokenHub.ClaimService.Api.Application;

public interface IClaimService
{
    Task<ClaimSummaryResponse> SubmitAsync(string userId, SubmitClaimRequest request, CancellationToken cancellationToken);

    Task<PagedResponse<ClaimSummaryResponse>> GetForUserAsync(string userId, int limit, int offset, CancellationToken cancellationToken);

    Task<PagedResponse<ClaimModeratorResponse>> GetPendingAsync(int limit, int offset, CancellationToken cancellationToken);

    Task<PagedResponse<ClaimModeratorResponse>> GetReviewedAsync(ClaimStatus? status, string? reviewerId, int limit, int offset, CancellationToken cancellationToken);

    Task<PublicClaimStatusResponse> GetPublicStatusAsync(string claimId, CancellationToken cancellationToken);

    Task<ClaimSummaryResponse> ReviewAsync(string claimId, string reviewerId, ReviewClaimRequest request, string correlationId, CancellationToken cancellationToken);

    Task<ClaimSummaryResponse> AppealAsync(string claimId, string userId, AppealClaimRequest request, CancellationToken cancellationToken);
}
