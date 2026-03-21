namespace Application.Orders.GetById;

public sealed record OrderDetailResponse(
    Guid Id,
    DateTime CreatedAt,
    decimal TotalAmount,
    List<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid CategoryId,
    string CategoryName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
