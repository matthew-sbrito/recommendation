using Application.Abstractions.Messaging;
using Application.Products;
using Application.Products.GetRecommendations;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products;

internal sealed class GetRecommendations : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("products/recommendations", async (
            int count,
            IQueryHandler<GetRecommendationsQuery, List<ProductResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRecommendationsQuery(count);

            Result<List<ProductResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Products)
        .RequireAuthorization();
    }
}
