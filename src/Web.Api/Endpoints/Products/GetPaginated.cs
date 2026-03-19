using Application.Abstractions.Messaging;
using Application.Products;
using Application.Products.GetPaginated;
using Application.Shared;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products;

internal sealed class GetPaginated : IEndpoint
{
    public sealed record Request(
        string? Search,
        Guid? CategoryId,
        decimal? MinPrice,
        decimal? MaxPrice,
        int Page = 1,
        int PageSize = 20);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("products", async (
            [AsParameters] Request request,
            IQueryHandler<GetProductsQuery, PagedResponse<ProductResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProductsQuery(
                request.Search,
                request.CategoryId,
                request.MinPrice,
                request.MaxPrice,
                request.Page,
                request.PageSize);

            Result<PagedResponse<ProductResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Products)
        .RequireAuthorization();
    }
}
