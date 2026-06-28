namespace Challenge.Domain.Entities.Accounts;

public record Currency
{
    public string Code { get; }
    public int Decimals { get; }

    private static readonly Dictionary<string, int> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        { "USD", 2 },
        { "ARS", 2 },
        { "CLP", 0 }
    };

    private Currency(string code, int decimals)
    {
        Code = code.ToUpperInvariant();
        Decimals = decimals;
    }

    public static bool TryCreate(string code, out Currency? currency)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            currency = null;
            return false;
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        if (AllowedCurrencies.TryGetValue(normalizedCode, out int decimals))
        {
            currency = new Currency(normalizedCode, decimals);
            return true;
        }

        currency = null;
        return false;
    }

    public static Currency FromCode(string code)
    {
        if (TryCreate(code, out var currency) && currency != null)
        {
            return currency;
        }
        throw new ArgumentException($"Unsupported or invalid currency code: {code}", nameof(code));
    }
}
