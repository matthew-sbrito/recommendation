namespace Application.Shared;

public sealed record PagedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize);
