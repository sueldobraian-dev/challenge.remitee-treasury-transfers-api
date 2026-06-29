namespace Challenge.Domain.Entities.Accounts;

public record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        }

        if (decimal.Round(amount, currency.Decimals, MidpointRounding.ToEven) != amount)
        {
            throw new ArgumentException($"Amount {amount} has invalid decimal precision for currency {currency.Code}. Expected {currency.Decimals} decimals.", nameof(amount));
        }

        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot add money of different currencies.");
        }
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot subtract money of different currencies.");
        }
        if (Amount - other.Amount < 0)
        {
            throw new InvalidOperationException("Insufficient funds resulting in a negative amount.");
        }
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal multiplier, Currency targetCurrency)
    {
        ArgumentNullException.ThrowIfNull(targetCurrency);

        if (multiplier <= 0)
        {
            throw new ArgumentException("Multiplier must be strictly positive.", nameof(multiplier));
        }

        var rawAmount = Amount * multiplier;
        var roundedAmount = Math.Round(rawAmount, targetCurrency.Decimals, MidpointRounding.ToEven);
        return new Money(roundedAmount, targetCurrency);
    }
}
