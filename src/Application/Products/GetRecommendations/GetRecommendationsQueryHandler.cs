using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Orders;
using Domain.Products;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SharedKernel;

namespace Application.Products.GetRecommendations;

internal sealed class GetRecommendationsQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetRecommendationsQuery, List<ProductResponse>>
{
    private static readonly Error NoProductToMetric = Error.Failure(
        "Recommendation.NoProducts",
        "No products found to recommend products for you.");

    public async Task<Result<List<ProductResponse>>> Handle(
        GetRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        Result<GetOrderedOrSimilarByUserResult> productsResult =
            await GetOrderedOrSimilarByUserAsync(cancellationToken);

        if (productsResult.IsFailure)
        {
            return Result.Failure<List<ProductResponse>>(productsResult.Error);
        }

        bool fromOrders = productsResult.Value.Ordered;
        List<Product> products = productsResult.Value.Products;

        Vector searchVector = ComputeAverageEmbedding(products.ConvertAll(x => x.Embedding));

        List<Guid> orderedProductIds = fromOrders
            ? products.ConvertAll(x => x.Id)
            : [];

        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => !orderedProductIds.Contains(p.Id))
            .OrderBy(p => p.Embedding.MaxInnerProduct(searchVector))
            .Take(query.Count)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.CategoryId,
                p.Category.Name,
                p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private sealed record GetOrderedOrSimilarByUserResult(List<Product> Products, bool Ordered);

    private async Task<Result<GetOrderedOrSimilarByUserResult>> GetOrderedOrSimilarByUserAsync(
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        List<Product> orderedProducts = await context.Orders
           .AsNoTracking()
           .Where(o => o.UserId == userId)
           .SelectMany(o => o.Items.Select(x => x.Product))
           .GroupBy(x => x.Id)
           .Select(x => x.First())
           .ToListAsync(cancellationToken);

        if (orderedProducts.Count > 0)
        {
            return new GetOrderedOrSimilarByUserResult(orderedProducts, true);
        }

        User? currentUser = await context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .SingleOrDefaultAsync(cancellationToken);

        if (currentUser is null)
        {
            return Result.Failure<GetOrderedOrSimilarByUserResult>(UserErrors.NotFound(userId));
        }

        int currentAge = CalculateAge(currentUser.BirthDate);
        var minBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-(currentAge + 3) - 1));
        var maxBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-(currentAge - 3)));


        List<Guid> similarUserIds = await context.Users
            .AsNoTracking()
            .Where(u => u.Id != userId
                    && u.Gender == currentUser.Gender
                    && u.BirthDate >= minBirth
                    && u.BirthDate <= maxBirth)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (similarUserIds.Count == 0)
        {
            return Result.Failure<GetOrderedOrSimilarByUserResult>(NoProductToMetric);
        }

        List<Guid> popularProductIds = await context.Orders
            .AsNoTracking()
            .Where(o => similarUserIds.Contains(o.UserId))
            .SelectMany(o => o.Items.Select(i => i.ProductId))
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .Take(20)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        if (popularProductIds.Count == 0)
        {
            return Result.Failure<GetOrderedOrSimilarByUserResult>(NoProductToMetric);
        }

        List<Product> products = await context.Products
            .AsNoTracking()
            .Where(p => popularProductIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return new GetOrderedOrSimilarByUserResult(products, false);
    }

    private static Vector ComputeAverageEmbedding(List<Vector> embeddings)
    {
        int dimensions = embeddings[0].Memory.Length;
        float[] avg = new float[dimensions];

        foreach (Vector v in embeddings)
        {
            ReadOnlySpan<float> span = v.Memory.Span;
            for (int i = 0; i < dimensions; i++)
            {
                avg[i] += span[i];
            }
        }

        for (int i = 0; i < dimensions; i++)
        {
            avg[i] /= embeddings.Count;
        }

        return new Vector(avg);
    }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        int age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
