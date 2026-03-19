using Application.Abstractions.Messaging;
using Application.Shared;

namespace Application.Products.GetPaginated;

public sealed record GetProductsQuery(
    string? Search,
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page,
    int PageSize) : IQuery<PagedResponse<ProductResponse>>;
