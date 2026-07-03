using Challenge.Application.Common.Events;
using Challenge.Application.Common.Interfaces;
using Challenge.Domain.Entities;
using Challenge.Domain.Entities.Accounts;
using Challenge.Domain.Repositories;

namespace Challenge.Application.Features.Transfers.Orchestration;

public class TransferSagaOrchestrator : ITransferSagaOrchestrator
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaService _kafkaService;

    public TransferSagaOrchestrator(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        IKafkaService kafkaService)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _kafkaService = kafkaService;
    }

    public async Task ExecuteAsync(
        LedgerTransaction transaction,
        Account sourceAccount,
        Account targetAccount,
        Money debitMoney,
        Money creditMoney,
        CancellationToken cancellationToken)
    {
        var startedEvent = new TransferStartedEvent(
            transaction.Id,
            transaction.OperationId,
            transaction.SourceAccountId,
            transaction.TargetAccountId,
            transaction.AmountDebited,
            transaction.AmountCredited,
            transaction.FxRate
        );
        await _kafkaService.PublishAsync("transfer-started", startedEvent, cancellationToken);

        try
        {
            sourceAccount.Debit(debitMoney);
            _accountRepository.Update(sourceAccount);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var debitedEvent = new SourceAccountDebitedEvent(
                transaction.Id,
                transaction.OperationId,
                transaction.SourceAccountId,
                transaction.AmountDebited
            );
            await _kafkaService.PublishAsync("source-account-debited", debitedEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            transaction.UpdateStatus("FAILED");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var failedEvent = new TransferFailedEvent(
                transaction.Id,
                transaction.OperationId,
                $"Debit failed: {ex.Message}"
            );
            await _kafkaService.PublishAsync("transfer-failed", failedEvent, cancellationToken);
            throw;
        }

        try
        {
            targetAccount.Credit(creditMoney);
            _accountRepository.Update(targetAccount);

            transaction.UpdateStatus("COMPLETED");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var completedEvent = new TransferCompletedEvent(
                transaction.Id,
                transaction.OperationId,
                transaction.SourceAccountId,
                transaction.TargetAccountId,
                transaction.AmountDebited,
                transaction.AmountCredited
            );
            await _kafkaService.PublishAsync("transfer-completed", completedEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            try
            {
                sourceAccount.Credit(debitMoney);
                _accountRepository.Update(sourceAccount);
            }
            catch (Exception)
            {
                // Ignore or log compensation failure, prioritize setting transaction to FAILED
            }

            transaction.UpdateStatus("FAILED");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var failedEvent = new TransferFailedEvent(
                transaction.Id,
                transaction.OperationId,
                $"Credit failed (compensated): {ex.Message}"
            );
            await _kafkaService.PublishAsync("transfer-failed", failedEvent, cancellationToken);
            throw;
        }
    }
}
