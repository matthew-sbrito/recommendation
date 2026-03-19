using Application.Abstractions.Messaging;

namespace Application.Orders.GetById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDetailResponse>;
