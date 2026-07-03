using Challenge.Application.Features.Transfers.Commands;
using Challenge.Domain.Entities;
using Challenge.Domain.Entities.Accounts;
using Challenge.Domain.Exceptions;
using Challenge.Domain.Repositories;
using FakeItEasy;
using FluentAssertions;

namespace Challenge.UnitTests;

public class CreateTransferCommandTests
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateTransferCommandHandler _handler;

    public CreateTransferCommandTests()
    {
        _accountRepository = A.Fake<IAccountRepository>();
        _transactionRepository = A.Fake<ITransactionRepository>();
        _unitOfWork = A.Fake<IUnitOfWork>();

        _handler = new CreateTransferCommandHandler(
            _accountRepository,
            _transactionRepository,
            _unitOfWork
        );
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateOperationId_ShouldThrowIdempotencyException()
    {
        // Arrange
        var operationId = Guid.NewGuid();
        var existingTx = new LedgerTransaction(
            Guid.NewGuid(),
            operationId,
            "ACC-USD-1",
            "ACC-USD-2",
            100.00m,
            100.00m,
            null,
            DateTimeOffset.UtcNow.AddMinutes(-5)
        );

        A.CallTo(() => _transactionRepository.GetByOperationIdAsync(operationId, A<CancellationToken>._))
            .Returns(existingTx);

        var command = new CreateTransferCommand(
            OperationId: operationId,
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<IdempotencyException>()
            .WithMessage("Duplicate transfer");

        A.CallTo(() => _accountRepository.GetByIdAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _transactionRepository.AddAsync(A<LedgerTransaction>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }



    [Fact]
    public async Task HandleAsync_WithSourceAccountNotFound_ShouldThrowAccountNotFoundException()
    {
        // Arrange
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._))
            .Returns(Task.FromResult<Account?>(null));

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AccountNotFoundException>()
            .WithMessage("*ACC-USD-1*");
    }

    [Fact]
    public async Task HandleAsync_WithTargetAccountNotFound_ShouldThrowAccountNotFoundException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var sourceAcc = new Account("ACC-USD-1", usd, 1000.00m, AccountStatus.Active);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._))
            .Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-2", A<CancellationToken>._))
            .Returns(Task.FromResult<Account?>(null));

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AccountNotFoundException>()
            .WithMessage("*ACC-USD-2*");
    }

    [Fact]
    public async Task HandleAsync_WithCurrencyMismatch_ShouldThrowCurrencyMismatchException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var sourceAcc = new Account("ACC-USD-1", usd, 1000.00m, AccountStatus.Active);
        var targetAcc = new Account("ACC-USD-2", usd, 500.00m, AccountStatus.Active);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._))
            .Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-2", A<CancellationToken>._))
            .Returns(targetAcc);

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "EUR",
            Fx: null
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CurrencyMismatchException>();
    }

    [Theory]
    [InlineData(AccountStatus.Frozen, AccountStatus.Active)]
    [InlineData(AccountStatus.Active, AccountStatus.Frozen)]
    [InlineData(AccountStatus.Frozen, AccountStatus.Frozen)]
    public async Task HandleAsync_WithFrozenAccount_ShouldThrowAccountIsFrozenException(AccountStatus sourceStatus, AccountStatus targetStatus)
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var sourceAcc = new Account("ACC-USD-1", usd, 1000.00m, sourceStatus);
        var targetAcc = new Account("ACC-USD-2", usd, 500.00m, targetStatus);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._))
            .Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-2", A<CancellationToken>._))
            .Returns(targetAcc);

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AccountIsFrozenException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    public async Task HandleAsync_WithCrossCurrencyAndInvalidFx_ShouldThrowFxRequiredException(double? fxValue)
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var clp = Currency.FromCode("CLP");
        var sourceAcc = new Account("ACC-USD-1", usd, 1000.00m, AccountStatus.Active);
        var targetAcc = new Account("ACC-CLP-1", clp, 500000m, AccountStatus.Active);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._))
            .Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-CLP-1", A<CancellationToken>._))
            .Returns(targetAcc);

        decimal? fx = fxValue.HasValue ? (decimal)fxValue.Value : null;

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-CLP-1",
            Amount: 100.00m,
            Currency: "USD",
            Fx: fx
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FxRequiredException>();
    }

    [Fact]
    public async Task HandleAsync_WithSameCurrencyAndFxProvided_ShouldThrowFxNotAllowedException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var sourceAcc = new Account("ACC-USD-1", usd, 1000.00m, AccountStatus.Active);
        var targetAcc = new Account("ACC-USD-2", usd, 500.00m, AccountStatus.Active);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._))
            .Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-2", A<CancellationToken>._))
            .Returns(targetAcc);

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: 1.0m
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FxNotAllowedException>();
    }

    [Fact]
    public async Task HandleAsync_WithInsufficientFunds_ShouldThrowInsufficientFundsException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var sourceAcc = new Account("ACC-USD-1", usd, 49.99m, AccountStatus.Active);
        var targetAcc = new Account("ACC-USD-2", usd, 500.00m, AccountStatus.Active);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._))
            .Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-2", A<CancellationToken>._))
            .Returns(targetAcc);

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 50.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task HandleAsync_WithValidSameCurrencyRequest_ShouldSucceed()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var sourceAcc = new Account("ACC-USD-1", usd, 1000.00m, AccountStatus.Active);
        var targetAcc = new Account("ACC-USD-2", usd, 500.00m, AccountStatus.Active);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._))
            .Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-2", A<CancellationToken>._))
            .Returns(targetAcc);

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AmountDebited.Should().Be(100.00m);
        result.AmountCredited.Should().Be(100.00m);
        
        sourceAcc.Balance.Should().Be(900.00m);
        targetAcc.Balance.Should().Be(600.00m);

        A.CallTo(() => _transactionRepository.AddAsync(A<LedgerTransaction>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _accountRepository.Update(sourceAcc)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _accountRepository.Update(targetAcc)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappened(3, Times.Exactly);
    }

    [Fact]
    public async Task HandleAsync_WithValidCrossCurrencyRequest_ShouldApplyFxAndSucceed()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var clp = Currency.FromCode("CLP");
        var sourceAcc = new Account("ACC-USD-1", usd, 100.00m, AccountStatus.Active);
        var targetAcc = new Account("ACC-CLP-1", clp, 0.00m, AccountStatus.Active);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._))
            .Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-CLP-1", A<CancellationToken>._))
            .Returns(targetAcc);

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-CLP-1",
            Amount: 10.05m,
            Currency: "USD",
            Fx: 850.5m // 10.05 * 850.5 = 8547.525. Banker's Rounding yields 8548.
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AmountDebited.Should().Be(10.05m);
        result.AmountCredited.Should().Be(8548m);

        sourceAcc.Balance.Should().Be(89.95m);
        targetAcc.Balance.Should().Be(8548m);

        A.CallTo(() => _transactionRepository.AddAsync(A<LedgerTransaction>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _accountRepository.Update(sourceAcc)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _accountRepository.Update(targetAcc)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappened(3, Times.Exactly);
    }

    [Fact]
    public async Task HandleAsync_WhenDebitFails_ShouldMarkTransactionAsFailedAndRethrow()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var sourceAcc = new Account("ACC-USD-1", usd, 1000.00m, AccountStatus.Active);
        var targetAcc = new Account("ACC-USD-2", usd, 500.00m, AccountStatus.Active);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._)).Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-2", A<CancellationToken>._)).Returns(targetAcc);

        // Make the 2nd SaveChangesAsync throw (which is during the Debit step)
        var saveCallCount = 0;
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).ReturnsLazily(() =>
        {
            saveCallCount++;
            if (saveCallCount == 2)
            {
                throw new Exception("Database write failed");
            }
            return 1;
        });

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappened(3, Times.Exactly); // 1 (pending) + 2 (failed debit) + 3 (set to FAILED)
    }

    [Fact]
    public async Task HandleAsync_WhenCreditFails_ShouldCompensateByRefundAndMarkAsFailedAndRethrow()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var sourceAcc = new Account("ACC-USD-1", usd, 1000.00m, AccountStatus.Active);
        var targetAcc = new Account("ACC-USD-2", usd, 500.00m, AccountStatus.Active);

        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-1", A<CancellationToken>._)).Returns(sourceAcc);
        A.CallTo(() => _accountRepository.GetByIdAsync("ACC-USD-2", A<CancellationToken>._)).Returns(targetAcc);

        // Make the 3rd SaveChangesAsync throw (which is during the Credit step)
        var saveCallCount = 0;
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).ReturnsLazily(() =>
        {
            saveCallCount++;
            if (saveCallCount == 3)
            {
                throw new Exception("Database write failed during credit");
            }
            return 1;
        });

        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        
        // Assert compensation occurred: source account balance should be refunded to original value
        sourceAcc.Balance.Should().Be(1000.00m); // 1000 - 100 (debit) + 100 (refund)
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappened(4, Times.Exactly); // 1 (pending) + 2 (debit) + 3 (failed credit) + 4 (compensation & set FAILED)
    }
}

