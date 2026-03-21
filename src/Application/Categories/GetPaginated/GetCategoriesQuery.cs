using Application.Abstractions.Messaging;
using SharedKernel.DTO;

namespace Application.Categories.GetPaginated;

public sealed record GetCategoriesQuery(
    string? Search,
    int Page,
    int PageSize) : IQuery<PagedResponse<CategoryResponse>>;
