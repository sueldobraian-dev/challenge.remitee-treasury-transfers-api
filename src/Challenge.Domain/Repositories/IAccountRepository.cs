using Challenge.Domain.Entities.Accounts;

namespace Challenge.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    void Update(Account account);
}
