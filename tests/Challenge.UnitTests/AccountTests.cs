using Challenge.Domain.Entities.Accounts;
using FluentAssertions;

namespace Challenge.UnitTests;

public class AccountTests
{
    [Fact]
    public void CreateAccount_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var usd = Currency.FromCode("USD");

        // Act
        var account = new Account("ACC-1", usd, 150.00m, AccountStatus.Active);

        // Assert
        account.Id.Should().Be("ACC-1");
        account.CurrencyCode.Should().Be("USD");
        account.Balance.Should().Be(150.00m);
        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void Debit_WithActiveStatusAndSufficientFunds_ShouldDecreaseBalance()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var account = new Account("ACC-1", usd, 500.00m, AccountStatus.Active);
        var amountToDebit = new Money(100.00m, usd);

        // Act
        account.Debit(amountToDebit);

        // Assert
        account.Balance.Should().Be(400.00m);
    }

    [Fact]
    public void Credit_WithActiveStatus_ShouldIncreaseBalance()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var account = new Account("ACC-1", usd, 500.00m, AccountStatus.Active);
        var amountToCredit = new Money(200.00m, usd);

        // Act
        account.Credit(amountToCredit);

        // Assert
        account.Balance.Should().Be(700.00m);
    }

    [Fact]
    public void Debit_WithFrozenStatus_ShouldThrowAccountIsFrozenException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var account = new Account("ACC-1", usd, 500.00m, AccountStatus.Frozen);
        var amountToDebit = new Money(100.00m, usd);

        // Act
        Action act = () => account.Debit(amountToDebit);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Credit_WithFrozenStatus_ShouldThrowAccountIsFrozenException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var account = new Account("ACC-1", usd, 500.00m, AccountStatus.Frozen);
        var amountToCredit = new Money(200.00m, usd);

        // Act
        Action act = () => account.Credit(amountToCredit);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}

