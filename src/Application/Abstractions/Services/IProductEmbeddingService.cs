using Domain.Products;
using SharedKernel;

namespace Application.Abstractions.Services;

public interface IProductEmbeddingService
{
    Task<Result<float[]>> GenerateEmbeddingAsync(Product product, CancellationToken cancellationToken);
}
