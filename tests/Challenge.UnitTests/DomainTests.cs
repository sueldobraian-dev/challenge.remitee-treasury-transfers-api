using FluentAssertions;
using Challenge.Domain.Entities.Accounts;

namespace Challenge.UnitTests;

public class DomainTests
{
    [Theory]
    [InlineData("USD", 2)]
    [InlineData("ARS", 2)]
    [InlineData("CLP", 0)]
    public void Currency_TryCreate_WithSupportedCode_ShouldSucceed(string code, int expectedDecimals)
    {
        // Act
        var result = Currency.TryCreate(code, out var currency);

        // Assert
        result.Should().BeTrue();
        currency.Should().NotBeNull();
        currency!.Code.Should().Be(code);
        currency.Decimals.Should().Be(expectedDecimals);
    }

    [Fact]
    public void Currency_TryCreate_WithUnsupportedCode_ShouldReturnFalse()
    {
        // Act
        var result = Currency.TryCreate("EUR", out var currency);

        // Assert
        result.Should().BeFalse();
        currency.Should().BeNull();
    }

    [Fact]
    public void Money_Constructor_WithInvalidDecimals_ShouldThrowArgumentException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");

        // Act & Assert
        Action act = () => new Money(100.005m, usd);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*invalid decimal precision*");
    }

    [Fact]
    public void Money_Constructor_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");

        // Act & Assert
        Action act = () => new Money(-10.00m, usd);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Theory]
    // Banker's Rounding (MidpointRounding.ToEven) checks:
    // USD (2 decimals)
    [InlineData(10.005, "USD", 10.00)] // Midpoint goes to nearest even: 0
    [InlineData(10.015, "USD", 10.02)] // Midpoint goes to nearest even: 2
    // CLP (0 decimals)
    [InlineData(100.5, "CLP", 100.00)] // Midpoint goes to nearest even: 100
    [InlineData(101.5, "CLP", 102.00)] // Midpoint goes to nearest even: 102
    public void Money_Multiply_ShouldApplyBankersRoundingCorrectly(decimal rawValue, string targetCurrencyCode, decimal expectedValue)
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var targetCurrency = Currency.FromCode(targetCurrencyCode);
        
        // Setup raw money with 1 unit
        var baseMoney = new Money(1.00m, usd); 

        // Act: multiply 1.00 by rawValue, which matches rawValue
        var result = baseMoney.Multiply(rawValue, targetCurrency);

        // Assert
        result.Amount.Should().Be(expectedValue);
        result.Currency.Code.Should().Be(targetCurrencyCode);
    }

    [Fact]
    public void Account_Debit_WithValidFunds_ShouldDecreaseBalance()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var account = new Account("ACC-USD-1", usd, 1000.00m, AccountStatus.Active);
        var debitAmount = new Money(150.00m, usd);

        // Act
        account.Debit(debitAmount);

        // Assert
        account.Balance.Should().Be(850.00m);
    }

    [Fact]
    public void Account_Debit_WithInsufficientFunds_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var account = new Account("ACC-USD-1", usd, 50.00m, AccountStatus.Active);
        var debitAmount = new Money(100.00m, usd);

        // Act & Assert
        Action act = () => account.Debit(debitAmount);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient funds.");
    }

    [Fact]
    public void Account_Debit_OnFrozenAccount_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var account = new Account("ACC-USD-1", usd, 500.00m, AccountStatus.Frozen);
        var debitAmount = new Money(100.00m, usd);

        // Act & Assert
        Action act = () => account.Debit(debitAmount);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot debit from a non-active account.");
    }
}
