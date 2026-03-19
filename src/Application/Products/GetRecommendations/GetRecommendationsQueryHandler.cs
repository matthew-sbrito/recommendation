using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
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
    public async Task<Result<List<ProductResponse>>> Handle(
        GetRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        List<Guid> orderedProductIds = await context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .SelectMany(o => o.Items.Select(i => i.ProductId))
            .Distinct()
            .ToListAsync(cancellationToken);

        Vector? searchVector = null;

        if (orderedProductIds.Count > 0)
        {
            List<Vector> orderedEmbeddings = await context.Products
                .AsNoTracking()
                .Where(p => orderedProductIds.Contains(p.Id))
                .Select(p => p.Embedding)
                .ToListAsync(cancellationToken);

            searchVector = ComputeAverageEmbedding(orderedEmbeddings);
        }
        else
        {
            User? currentUser = await context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .SingleOrDefaultAsync(cancellationToken);

            if (currentUser is null)
            {
                return Result.Failure<List<ProductResponse>>(UserErrors.NotFound(userId));
            }

            int currentAge = CalculateAge(currentUser.BirthDate);
            var minBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-(currentAge + 5) - 1));
            var maxBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-(currentAge - 5)));

            List<Guid> similarUserIds = await context.Users
                .AsNoTracking()
                .Where(u => u.Id != userId
                         && u.Gender == currentUser.Gender
                         && u.BirthDate >= minBirth
                         && u.BirthDate <= maxBirth)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (similarUserIds.Count > 0)
            {
                List<Guid> popularProductIds = await context.Orders
                    .AsNoTracking()
                    .Where(o => similarUserIds.Contains(o.UserId))
                    .SelectMany(o => o.Items.Select(i => i.ProductId))
                    .GroupBy(id => id)
                    .OrderByDescending(g => g.Count())
                    .Take(20)
                    .Select(g => g.Key)
                    .ToListAsync(cancellationToken);

                if (popularProductIds.Count > 0)
                {
                    List<Vector> popularEmbeddings = await context.Products
                        .AsNoTracking()
                        .Where(p => popularProductIds.Contains(p.Id))
                        .Select(p => p.Embedding)
                        .ToListAsync(cancellationToken);

                    searchVector = ComputeAverageEmbedding(popularEmbeddings);
                }
            }

            if (searchVector is null)
            {
                return await GetGloballyPopularProductsAsync(query.Count, [], cancellationToken);
            }
        }

        List<ProductResponse> recommendations = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => !orderedProductIds.Contains(p.Id))
            .OrderBy(p => p.Embedding.CosineDistance(searchVector))
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

        return recommendations;
    }

    private async Task<Result<List<ProductResponse>>> GetGloballyPopularProductsAsync(
        int count,
        List<Guid> excludeIds,
        CancellationToken cancellationToken)
    {
        List<Guid> popularIds = await context.OrderItems
            .AsNoTracking()
            .Where(oi => !excludeIds.Contains(oi.ProductId))
            .GroupBy(oi => oi.ProductId)
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        List<ProductResponse> products = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => popularIds.Contains(p.Id))
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

        return products;
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
