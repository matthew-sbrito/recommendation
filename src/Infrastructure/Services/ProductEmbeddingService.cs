using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using Application.Abstractions.Services;
using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel;

namespace Infrastructure.Services;

internal sealed class ProductEmbeddingService(IHttpClientFactory httpClientFactory) : IProductEmbeddingService
{
    private readonly static Error UnexpectedError = Error.Failure(
        "OpenRouter.Embedding",
        "Error while trying embedding product using OpenRouter.");

    public record OpenAIEmbeddingResponse(List<EmbeddingData> Data);

    public record EmbeddingData(float[] Embedding);

    public async Task<Result<float[]>> GenerateEmbeddingAsync(Product product)
    {
        HttpClient client = httpClientFactory.CreateClient("OpenRouter");

        var request = new
        {
            model = "mistral/mistral-embed", // one of the few that supports embeddings
            input = GenerateText(product),
            encoding_format = "float",
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/embeddings", request);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<float[]>(UnexpectedError);
        }

        OpenAIEmbeddingResponse? result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>();

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
            Description: {product.Category.Description}
            Price: {product.Price.Amount} {product.Price.Currency}
        """;
    }
}
