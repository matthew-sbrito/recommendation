using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Shared;
using Domain.Categories;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Categories.GetPaginated;

internal sealed class GetCategoriesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetCategoriesQuery, PagedResponse<CategoryResponse>>
{
    public async Task<Result<PagedResponse<CategoryResponse>>> Handle(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Category> categoriesQuery = context.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            categoriesQuery = categoriesQuery.Where(c => c.Name.Contains(query.Search));
        }

        int totalCount = await categoriesQuery.CountAsync(cancellationToken);

        List<CategoryResponse> items = await categoriesQuery
            .OrderBy(c => c.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description))
            .ToListAsync(cancellationToken);

        return new PagedResponse<CategoryResponse>(items, totalCount, query.Page, query.PageSize);
    }
}
