using Challenge.Application.Common.DispatchR;
using Challenge.Application.Features.Transfers.Commands.Responses;
using Challenge.Domain.Entities;
using Challenge.Domain.Entities.Accounts;
using Challenge.Domain.Exceptions;
using Challenge.Domain.Repositories;

namespace Challenge.Application.Features.Transfers.Commands;

public class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, TransferResultResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransferCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TransferResultResponse> HandleAsync(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        var existingTransaction = await _transactionRepository.GetByOperationIdAsync(command.OperationId, cancellationToken);
        if (existingTransaction != null)
        {
            throw new IdempotencyException("Duplicate transfer");
        }

        var sourceAccount = await _accountRepository.GetByIdAsync(command.SourceAccountId, cancellationToken) ?? throw new AccountNotFoundException(command.SourceAccountId);
        var targetAccount = await _accountRepository.GetByIdAsync(command.TargetAccountId, cancellationToken) ?? throw new AccountNotFoundException(command.TargetAccountId);

        if (!string.Equals(command.Currency, sourceAccount.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new CurrencyMismatchException(command.Currency, sourceAccount.CurrencyCode);
        }

        if (sourceAccount.Status == AccountStatus.Frozen || targetAccount.Status == AccountStatus.Frozen)
        {
            throw new AccountIsFrozenException();
        }

        bool isCrossCurrency = !string.Equals(sourceAccount.CurrencyCode, targetAccount.CurrencyCode, StringComparison.OrdinalIgnoreCase);
        decimal finalFxRate = 1.0m;

        if (isCrossCurrency)
        {
            if (!command.Fx.HasValue || command.Fx.Value <= 0)
            {
                throw new FxRequiredException();
            }
            finalFxRate = command.Fx.Value;
        }
        else
        {
            if (command.Fx.HasValue)
            {
                throw new FxNotAllowedException();
            }
        }

        var sourceCurrency = Currency.FromCode(sourceAccount.CurrencyCode);
        var targetCurrency = Currency.FromCode(targetAccount.CurrencyCode);

        var debitMoney = new Money(command.Amount, sourceCurrency);

        if (sourceAccount.Balance < debitMoney.Amount)
        {
            throw new InsufficientFundsException();
        }

        var creditMoney = debitMoney.Multiply(finalFxRate, targetCurrency);

        sourceAccount.Debit(debitMoney);
        targetAccount.Credit(creditMoney);

        var transactionId = Guid.NewGuid();
        var ledgerTransaction = new LedgerTransaction(
            transactionId,
            command.OperationId,
            sourceAccount.Id,
            targetAccount.Id,
            debitMoney.Amount,
            creditMoney.Amount,
            isCrossCurrency ? finalFxRate : null,
            DateTimeOffset.UtcNow
        );

        await _transactionRepository.AddAsync(ledgerTransaction, cancellationToken);
        _accountRepository.Update(sourceAccount);
        _accountRepository.Update(targetAccount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new TransferResultResponse(
            ledgerTransaction.Id,
            ledgerTransaction.OperationId,
            ledgerTransaction.Status,
            ledgerTransaction.SourceAccountId,
            ledgerTransaction.TargetAccountId,
            ledgerTransaction.AmountDebited,
            ledgerTransaction.AmountCredited,
            ledgerTransaction.CreatedAt
        );

        return result;
    }
}

