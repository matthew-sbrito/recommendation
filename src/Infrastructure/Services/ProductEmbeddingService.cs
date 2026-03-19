using System.Net.Http.Json;
using Application.Abstractions.Services;
using Domain.Products;
using SharedKernel;

namespace Infrastructure.Services;

internal sealed class ProductEmbeddingService(IHttpClientFactory httpClientFactory) : IProductEmbeddingService
{
    private static readonly Error UnexpectedError = Error.Failure(
        "OpenRouter.Embedding",
        "Error while trying embedding product using OpenRouter.");

    public record OpenAIEmbeddingResponse(List<EmbeddingData> Data);

    public record EmbeddingData(float[] Embedding);

    public async Task<Result<float[]>> GenerateEmbeddingAsync(Product product, CancellationToken cancellationToken)
    {
        HttpClient client = httpClientFactory.CreateClient("OpenRouter");

        var request = new
        {
            model = "mistral/mistral-embed", // one of the few that supports embeddings
            input = GenerateText(product),
            encoding_format = "float",
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/embeddings", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<float[]>(UnexpectedError);
        }

        OpenAIEmbeddingResponse? result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(cancellationToken);

        if (result is null)
        {
            return Result.Failure<float[]>(UnexpectedError);
        }

        return result.Data[0].Embedding;
    }

    private static string GenerateText(Product product)
    {
        return $"""
            Name: {product.Name}
            Category: {product.Category.Name}
            Description: {product.Description}
            Price: {product.Price.Amount} {product.Price.Currency}
        """;
    }
}
