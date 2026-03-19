using Application.Abstractions.Messaging;
using Application.Orders.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Orders;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("orders/{orderId:guid}", async (
            Guid orderId,
            IQueryHandler<GetOrderByIdQuery, OrderDetailResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrderByIdQuery(orderId);

            Result<OrderDetailResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Orders)
        .RequireAuthorization();
    }
}
