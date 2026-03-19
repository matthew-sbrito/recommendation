using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Orders.CreateOrder;

internal sealed class CreateOrderCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        List<Guid> productIds = command.Items.Select(i => i.ProductId).ToList();

        List<Product> products = await context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (Guid id in productIds)
        {
            if (!products.Any(p => p.Id == id))
            {
                return Result.Failure<Guid>(ProductErrors.NotFound(id));
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userContext.UserId,
            CreatedAt = dateTimeProvider.UtcNow,
            Items = command.Items.Select(item =>
            {
                Product product = products.First(p => p.Id == item.ProductId);
                return new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price.Amount
                };
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
