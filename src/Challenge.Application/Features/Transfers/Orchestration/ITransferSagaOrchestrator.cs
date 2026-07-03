using Challenge.Domain.Entities;
using Challenge.Domain.Entities.Accounts;

namespace Challenge.Application.Features.Transfers.Orchestration;

public interface ITransferSagaOrchestrator
{
    Task ExecuteAsync(
        LedgerTransaction transaction,
        Account sourceAccount,
        Account targetAccount,
        Money debitMoney,
        Money creditMoney,
        CancellationToken cancellationToken);
}
