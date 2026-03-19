using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Orders.GetById;

internal sealed class GetOrderByIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
    : IQueryHandler<GetOrderByIdQuery, OrderDetailResponse>
{
    public async Task<Result<OrderDetailResponse>> Handle(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken)
    {
        OrderDetailResponse? order = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == query.OrderId && o.UserId == userContext.UserId)
            .Select(o => new OrderDetailResponse(
                o.Id,
                o.CreatedAt,
                o.TotalAmount,
                o.Items.Select(i => new OrderItemResponse(
                    i.Id,
                    i.ProductId,
                    i.Product.Name,
                    i.Quantity,
                    i.UnitPrice,
                    i.Quantity * i.UnitPrice)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderDetailResponse>(OrderErrors.NotFound(query.OrderId));
        }

        return order;
    }
}
