namespace Application.Orders.GetHistory;

public sealed record OrderSummaryResponse(
    Guid Id,
    DateTime CreatedAt,
    decimal TotalAmount,
    int ItemCount);
