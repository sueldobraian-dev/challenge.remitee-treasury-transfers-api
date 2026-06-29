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

        // 1. Idempotency Check
        var existingTransaction = await _transactionRepository.GetByOperationIdAsync(command.OperationId, cancellationToken);
        if (existingTransaction != null)
        {
            throw new IdempotencyException("Duplicate transfer");
        }

        // 2. Load Accounts
        var sourceAccount = await _accountRepository.GetByIdAsync(command.SourceAccountId, cancellationToken);
        if (sourceAccount == null)
        {
            throw new AccountNotFoundException(command.SourceAccountId);
        }

        var targetAccount = await _accountRepository.GetByIdAsync(command.TargetAccountId, cancellationToken);
        if (targetAccount == null)
        {
            throw new AccountNotFoundException(command.TargetAccountId);
        }

        // 3. Validate request currency against source account currency
        if (!string.Equals(command.Currency, sourceAccount.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new CurrencyMismatchException(command.Currency, sourceAccount.CurrencyCode);
        }

        // 4. Account status validations (Frozen accounts cannot operate)
        if (sourceAccount.Status == AccountStatus.Frozen || targetAccount.Status == AccountStatus.Frozen)
        {
            throw new AccountIsFrozenException();
        }

        // 5. Currency match and FX validation
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

        // 6. Balance and Money setup
        var sourceCurrency = Currency.FromCode(sourceAccount.CurrencyCode);
        var targetCurrency = Currency.FromCode(targetAccount.CurrencyCode);

        var debitMoney = new Money(command.Amount, sourceCurrency);

        if (sourceAccount.Balance < debitMoney.Amount)
        {
            throw new InsufficientFundsException();
        }

        // Calculate credit money (applies Banker's Rounding implicitly inside Money.Multiply)
        var creditMoney = debitMoney.Multiply(finalFxRate, targetCurrency);


        // 7. Execute Mutations
        sourceAccount.Debit(debitMoney);
        targetAccount.Credit(creditMoney);

        // 8. Record Ledger Transaction
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

        // 9. Persist Atomic Changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);


        // 10. Return response payload
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

