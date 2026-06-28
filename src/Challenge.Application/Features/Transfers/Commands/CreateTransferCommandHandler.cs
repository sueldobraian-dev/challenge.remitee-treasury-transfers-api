using Challenge.Application.Common;
using Challenge.Application.Common.DispatchR;
using Challenge.Domain.Entities;
using Challenge.Domain.Exceptions;
using Challenge.Domain.Repositories;
using Challenge.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Challenge.Application.Features.Transfers.Commands;

public class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, TransferResultResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTransferCommandHandler> _logger;

    public CreateTransferCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateTransferCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransferResultResponse> HandleAsync(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing transfer command for operation '{OperationId}'", command.OperationId);

        // 1. Idempotency Check
        var existingTransaction = await _transactionRepository.GetByOperationIdAsync(command.OperationId, cancellationToken);
        if (existingTransaction != null)
        {
            _logger.LogInformation("Duplicate transfer request detected. Returning original successful transaction for operation '{OperationId}'", command.OperationId);
            throw new IdempotencyException("Duplicate transfer");
        }

        // 2. Initial input validations (Throws custom exceptions)
        if (command.Amount <= 0)
        {
            _logger.LogWarning("Validation failed for operation '{OperationId}': Amount {Amount} must be positive", command.OperationId, command.Amount);
            throw new InvalidAmountException();
        }

        if (string.Equals(command.SourceAccountId, command.TargetAccountId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Validation failed for operation '{OperationId}': Source and Target accounts are identical '{AccountId}'", command.OperationId, command.SourceAccountId);
            throw new IdenticalAccountsException();
        }

        // 3. Load Accounts
        var sourceAccount = await _accountRepository.GetByIdAsync(command.SourceAccountId, cancellationToken);
        if (sourceAccount == null)
        {
            _logger.LogWarning("Validation failed for operation '{OperationId}': Source account '{SourceAccountId}' not found", command.OperationId, command.SourceAccountId);
            throw new AccountNotFoundException(command.SourceAccountId);
        }

        var targetAccount = await _accountRepository.GetByIdAsync(command.TargetAccountId, cancellationToken);
        if (targetAccount == null)
        {
            _logger.LogWarning("Validation failed for operation '{OperationId}': Target account '{TargetAccountId}' not found", command.OperationId, command.TargetAccountId);
            throw new AccountNotFoundException(command.TargetAccountId);
        }

        // 4. Validate request currency against source account currency
        if (!string.Equals(command.Currency, sourceAccount.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Validation failed for operation '{OperationId}': Currency mismatch. Requested '{Requested}', Source Account has '{Actual}'", command.OperationId, command.Currency, sourceAccount.CurrencyCode);
            throw new CurrencyMismatchException(command.Currency, sourceAccount.CurrencyCode);
        }

        // 5. Account status validations (Frozen accounts cannot operate)
        if (sourceAccount.Status == AccountStatus.Frozen || targetAccount.Status == AccountStatus.Frozen)
        {
            _logger.LogWarning("Validation failed for operation '{OperationId}': Account status invalid. Source status: {SourceStatus}, Target status: {TargetStatus}", command.OperationId, sourceAccount.Status, targetAccount.Status);
            throw new AccountIsFrozenException();
        }

        // 6. Currency match and FX validation
        bool isCrossCurrency = !string.Equals(sourceAccount.CurrencyCode, targetAccount.CurrencyCode, StringComparison.OrdinalIgnoreCase);
        decimal finalFxRate = 1.0m;

        if (isCrossCurrency)
        {
            if (!command.Fx.HasValue || command.Fx.Value <= 0)
            {
                _logger.LogWarning("Validation failed for operation '{OperationId}': Cross-currency transfer without valid FX rate", command.OperationId);
                throw new FxRequiredException();
            }
            finalFxRate = command.Fx.Value;
        }
        else
        {
            if (command.Fx.HasValue)
            {
                _logger.LogWarning("Validation failed for operation '{OperationId}': Same-currency transfer provided with FX rate", command.OperationId);
                throw new FxNotAllowedException();
            }
        }

        // 7. Balance and Money setup
        var sourceCurrency = Currency.FromCode(sourceAccount.CurrencyCode);
        var targetCurrency = Currency.FromCode(targetAccount.CurrencyCode);

        var debitMoney = new Money(command.Amount, sourceCurrency);

        if (sourceAccount.Balance < debitMoney.Amount)
        {
            _logger.LogWarning("Validation failed for operation '{OperationId}': Insufficient funds. Balance: {Balance}, Requested: {Amount}", command.OperationId, sourceAccount.Balance, debitMoney.Amount);
            throw new InsufficientFundsException();
        }

        // Calculate credit money (applies Banker's Rounding implicitly inside Money.Multiply)
        var creditMoney = debitMoney.Multiply(finalFxRate, targetCurrency);

        _logger.LogInformation("Performing transfer for operation '{OperationId}': Debiting {DebitAmount} {SourceCurrency} from '{SourceAccountId}', Crediting {CreditAmount} {TargetCurrency} to '{TargetAccountId}' with FX rate {FxRate}",
            command.OperationId, debitMoney.Amount, sourceCurrency.Code, sourceAccount.Id, creditMoney.Amount, targetCurrency.Code, targetAccount.Id, finalFxRate);

        // 8. Execute Mutations
        sourceAccount.Debit(debitMoney);
        targetAccount.Credit(creditMoney);

        // 9. Record Ledger Transaction
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

        // 10. Persist Atomic Changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Transfer persisted successfully for operation '{OperationId}'. TxId: '{TxId}'", command.OperationId, ledgerTransaction.Id);

        // 11. Return response payload
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

