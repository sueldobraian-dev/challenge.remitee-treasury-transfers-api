using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ChallengeDbContext _context;

    public TransactionRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerTransaction?> GetByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerTransactions.FirstOrDefaultAsync(t => t.OperationId == operationId, cancellationToken);
    }

    public async Task AddAsync(LedgerTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.LedgerTransactions.AddAsync(transaction, cancellationToken);
    }
}
