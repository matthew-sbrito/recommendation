using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Shared;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Products.GetPaginated;

internal sealed class GetProductsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetProductsQuery, PagedResponse<ProductResponse>>
{
    public async Task<Result<PagedResponse<ProductResponse>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Product> productsQuery = context.Products
            .AsNoTracking()
            .Include(p => p.Category);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            productsQuery = productsQuery.Where(p =>
                p.Name.Contains(query.Search) || p.Description.Contains(query.Search));
        }

        if (query.CategoryId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        if (query.MinPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price.Amount >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Price.Amount <= query.MaxPrice.Value);
        }

        int totalCount = await productsQuery.CountAsync(cancellationToken);

        List<ProductResponse> items = await productsQuery
            .OrderBy(p => p.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.CategoryId,
                p.Category.Name,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProductResponse>(items, totalCount, query.Page, query.PageSize);
    }
}
