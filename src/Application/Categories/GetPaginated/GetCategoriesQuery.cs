using Application.Abstractions.Messaging;
using Application.Shared;

namespace Application.Categories.GetPaginated;

public sealed record GetCategoriesQuery(
    string? Search,
    int Page,
    int PageSize) : IQuery<PagedResponse<CategoryResponse>>;
