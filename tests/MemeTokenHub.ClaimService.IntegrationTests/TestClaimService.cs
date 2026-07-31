using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Domain;
using MemeTokenHub.ClaimService.Api.Dtos;

namespace MemeTokenHub.ClaimService.IntegrationTests;

internal sealed class TestClaimService : IClaimService
{
    public Task<PublicClaimStatusResponse> GetPublicStatusAsync(string claimId, CancellationToken cancellationToken) =>
        Task.FromResult(new PublicClaimStatusResponse(
            claimId,
            "user-1",
            "token-1",
            ClaimType.ProjectOwnership,
            ClaimStatus.Approved,
            DateTimeOffset.Parse("2026-07-30T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-07-31T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture)));

    public Task<ClaimSummaryResponse> SubmitAsync(string userId, SubmitClaimRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<PagedResponse<ClaimSummaryResponse>> GetForUserAsync(string userId, int limit, int offset, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<PagedResponse<ClaimModeratorResponse>> GetPendingAsync(int limit, int offset, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<PagedResponse<ClaimModeratorResponse>> GetReviewedAsync(ClaimStatus? status, string? reviewerId, int limit, int offset, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<ClaimSummaryResponse> ReviewAsync(string claimId, string reviewerId, ReviewClaimRequest request, string correlationId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<ClaimSummaryResponse> AppealAsync(string claimId, string userId, AppealClaimRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
}
