namespace MemeTokenHub.ClaimService.Api.Dtos;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Limit, int Offset, long Total);
