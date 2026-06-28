using Challenge.Domain.Aggregates;

namespace Challenge.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    void Update(Account account);
}
