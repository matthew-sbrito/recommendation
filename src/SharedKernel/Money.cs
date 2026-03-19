namespace SharedKernel;

public sealed class Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            return Result.Failure<Money>(Error.Failure("Money.NegativeAmount", "Amount cannot be negative."));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result.Failure<Money>(Error.Failure("Money.InvalidCurrency", "Currency must be a 3-letter ISO code."));

        return Result.Success(new Money(Math.Round(amount, 2), currency.ToUpperInvariant()));
    }

    public static Money Zero(string currency = "USD") => new(0m, currency);

    public Money Add(Money other)
    {
        GuardSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        GuardSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Math.Round(Amount * factor, 2), Currency);

    private void GuardSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot operate on {Currency} and {other.Currency}.");
    }

    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public int CompareTo(Money? other) => other is null ? 1 : Amount.CompareTo(other.Amount);
    public override string ToString() => $"{Currency} {Amount:F2}";

    public static bool operator ==(Money? l, Money? r) => Equals(l, r);
    public static bool operator !=(Money? l, Money? r) => !Equals(l, r);
    public static bool operator >(Money l, Money r) => l.CompareTo(r) > 0;
    public static bool operator <(Money l, Money r) => l.CompareTo(r) < 0;
    public static Money operator +(Money l, Money r) => l.Add(r);
    public static Money operator -(Money l, Money r) => l.Subtract(r);
}
