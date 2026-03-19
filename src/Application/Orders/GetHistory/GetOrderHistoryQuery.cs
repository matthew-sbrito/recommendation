using Application.Abstractions.Messaging;

namespace Application.Orders.GetHistory;

public sealed record GetOrderHistoryQuery : IQuery<List<OrderSummaryResponse>>;
