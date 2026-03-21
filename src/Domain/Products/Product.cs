using Domain.Categories;
using Pgvector;
using SharedKernel;
using SharedKernel.ValueObject;

namespace Domain.Products;

public sealed class Product : Entity
{

    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public Money Price { get; set; }

    public Guid CategoryId { get; set; }

    public Category Category { get; set; }

    public Vector Embedding { get; set; }

    public DateTime CreatedAt { get; set; }

    public string GetEmbeddingText()
    {
        return $"""
            Name: {Name}
            Category: {Category.Name}
            Description: {Description}
            Price: {Price.Amount} {Price.Currency}
        """;
    }
}
