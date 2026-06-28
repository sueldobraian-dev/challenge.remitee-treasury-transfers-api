using Challenge.Domain.Entities;

namespace Challenge.Domain.Repositories;

public interface ITransactionRepository
{
    Task<LedgerTransaction?> GetByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default);
    Task AddAsync(LedgerTransaction transaction, CancellationToken cancellationToken = default);
}
