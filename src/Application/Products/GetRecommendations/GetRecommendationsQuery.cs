using Application.Abstractions.Messaging;

namespace Application.Products.GetRecommendations;

public sealed record GetRecommendationsQuery(int Count = 10) : IQuery<List<ProductResponse>>;
