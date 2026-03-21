using SharedKernel.Errors;

namespace Domain.Orders;

public static class OrderErrors
{
    public static Error NotFound(Guid orderId) => Error.NotFound(
        "Orders.NotFound",
        $"The order with the Id = '{orderId}' was not found");

    public static readonly Error EmptyItems = Error.Problem(
        "Orders.EmptyItems",
        "An order must contain at least one item.");
}
