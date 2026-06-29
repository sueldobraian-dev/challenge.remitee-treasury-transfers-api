namespace Challenge.Domain.Entities;

public class LedgerTransaction
{
    public Guid Id { get; private set; }
    public Guid OperationId { get; private set; }
    public string SourceAccountId { get; private set; } = null!;
    public string TargetAccountId { get; private set; } = null!;
    public decimal AmountDebited { get; private set; }
    public decimal AmountCredited { get; private set; }
    public decimal? FxRate { get; private set; }
    public string Status { get; private set; } = "COMPLETED";
    public DateTimeOffset CreatedAt { get; private set; }

    private LedgerTransaction() { }

    public LedgerTransaction(
        Guid id,
        Guid operationId,
        string sourceAccountId,
        string targetAccountId,
        decimal amountDebited,
        decimal amountCredited,
        decimal? fxRate,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Transaction ID cannot be empty.", nameof(id));

        if (operationId == Guid.Empty)
            throw new ArgumentException("Operation ID (Idempotency Key) cannot be empty.", nameof(operationId));

        if (string.IsNullOrWhiteSpace(sourceAccountId))
            throw new ArgumentException("Source account ID cannot be empty.", nameof(sourceAccountId));

        if (string.IsNullOrWhiteSpace(targetAccountId))
            throw new ArgumentException("Target account ID cannot be empty.", nameof(targetAccountId));

        if (amountDebited <= 0)
            throw new ArgumentException("Amount debited must be strictly positive.", nameof(amountDebited));

        if (amountCredited <= 0)
            throw new ArgumentException("Amount credited must be strictly positive.", nameof(amountCredited));

        if (fxRate.HasValue && fxRate.Value <= 0)
            throw new ArgumentException("FX rate must be positive.", nameof(fxRate));

        Id = id;
        OperationId = operationId;
        SourceAccountId = sourceAccountId;
        TargetAccountId = targetAccountId;
        AmountDebited = amountDebited;
        AmountCredited = amountCredited;
        FxRate = fxRate;
        Status = "COMPLETED";
        CreatedAt = createdAt;
    }
}
