using Domain.Products;
using SharedKernel;

namespace Application.Abstractions.Services;

public interface IEmbeddingService
{
    Task<Result<float[]>> GenerateEmbeddingAsync(
        string content, CancellationToken cancellationToken);
}
