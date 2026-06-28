using System.Threading;
using System.Threading.Tasks;

namespace Challenge.Domain.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
