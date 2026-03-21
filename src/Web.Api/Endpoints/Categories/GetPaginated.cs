using Application.Abstractions.Messaging;
using Application.Categories.GetPaginated;
using SharedKernel;
using SharedKernel.DTO;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class GetPaginated : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("categories", async (
            string? search,
            int page,
            int pageSize,
            IQueryHandler<GetCategoriesQuery, PagedResponse<CategoryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCategoriesQuery(search, page, pageSize);

            Result<PagedResponse<CategoryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .RequireAuthorization();
    }
}
