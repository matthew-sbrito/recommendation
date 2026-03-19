using Application.Abstractions.Messaging;

namespace Application.Orders.CreateOrder;

public sealed record CreateOrderCommand(List<CreateOrderItemRequest> Items) : ICommand<Guid>;

public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);
