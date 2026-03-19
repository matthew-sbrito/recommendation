using Application.Abstractions.Messaging;
using Application.Orders.GetHistory;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Orders;

internal sealed class GetHistory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("orders", async (
            IQueryHandler<GetOrderHistoryQuery, List<OrderSummaryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrderHistoryQuery();

            Result<List<OrderSummaryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Orders)
        .RequireAuthorization();
    }
}
