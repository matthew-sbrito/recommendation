using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Orders.GetHistory;

internal sealed class GetOrderHistoryQueryHandler(IApplicationDbContext context, IUserContext userContext)
    : IQueryHandler<GetOrderHistoryQuery, List<OrderSummaryResponse>>
{
    public async Task<Result<List<OrderSummaryResponse>>> Handle(
        GetOrderHistoryQuery query,
        CancellationToken cancellationToken)
    {
        List<OrderSummaryResponse> orders = await context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userContext.UserId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryResponse(
                o.Id,
                o.CreatedAt,
                o.TotalAmount,
                o.Items.Count))
            .ToListAsync(cancellationToken);

        return orders;
    }
}
