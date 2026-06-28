using Challenge.Domain.Aggregates;
using Challenge.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ChallengeDbContext _context;

    public AccountRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public void Update(Account account)
    {
        _context.Accounts.Update(account);
    }
}
