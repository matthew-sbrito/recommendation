using Application.Abstractions.Messaging;
using Application.Orders.CreateOrder;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Orders;

internal sealed class Create : IEndpoint
{
    public sealed record Request(List<OrderItemRequest> Items);

    public sealed record OrderItemRequest(Guid ProductId, int Quantity);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("orders", async (
            Request request,
            ICommandHandler<CreateOrderCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateOrderCommand(
                request.Items.Select(i => new CreateOrderItemRequest(i.ProductId, i.Quantity)).ToList());

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Orders)
        .RequireAuthorization();
    }
}
