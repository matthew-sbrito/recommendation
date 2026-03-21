using System.Net.Http.Json;
using Application.Abstractions.Services;
using SharedKernel;
using SharedKernel.Errors;

namespace Infrastructure.Services;

internal sealed class OpenRouterEmbeddingService(
    IHttpClientFactory httpClientFactory) : IEmbeddingService
{
    private static readonly Error UnexpectedError = Error.Failure(
        "OpenRouter.Embedding",
        "Error while generating embedding using OpenRouter.");

    public record OpenAIEmbeddingResponse(List<EmbeddingData> Data);

    public record EmbeddingData(float[] Embedding);

    public async Task<Result<float[]>> GenerateEmbeddingAsync(
        string content, CancellationToken cancellationToken)
    {
        HttpClient client = httpClientFactory.CreateClient("OpenRouter");

        var request = new
        {
            model = "mistral/mistral-embed", // one of the few that supports embeddings
            input = content,
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
}
