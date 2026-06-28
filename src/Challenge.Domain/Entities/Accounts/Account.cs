namespace Challenge.Domain.Entities.Accounts;

public class Account
{
    public string Id { get; private set; } = null!;
    public string CurrencyCode { get; private set; } = null!;
    public decimal Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    public byte[] Version { get; private set; } = null!;

    // Constructor for Entity Framework Core
    private Account() { }

    public Account(string id, Currency currency, decimal balance, AccountStatus status)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Account ID cannot be empty.", nameof(id));
        
        ArgumentNullException.ThrowIfNull(currency);

        if (balance < 0)
            throw new ArgumentException("Initial balance cannot be negative.", nameof(balance));

        Id = id;
        CurrencyCode = currency.Code;
        Balance = balance;
        Status = status;
        Version = Array.Empty<byte>();
    }

    public void Debit(Money money)
    {
        ArgumentNullException.ThrowIfNull(money);

        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException("Cannot debit from a non-active account.");
        }

        if (money.Currency.Code != CurrencyCode)
        {
            throw new InvalidOperationException("Currency mismatch during debit.");
        }

        if (Balance < money.Amount)
        {
            throw new InvalidOperationException("Insufficient funds.");
        }

        Balance -= money.Amount;
    }

    public void Credit(Money money)
    {
        ArgumentNullException.ThrowIfNull(money);

        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException("Cannot credit to a non-active account.");
        }

        if (money.Currency.Code != CurrencyCode)
        {
            throw new InvalidOperationException("Currency mismatch during credit.");
        }

        Balance += money.Amount;
    }
}
