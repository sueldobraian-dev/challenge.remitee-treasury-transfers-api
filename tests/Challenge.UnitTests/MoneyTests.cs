using Challenge.Domain.Entities.Accounts;
using FluentAssertions;

namespace Challenge.UnitTests;

public class MoneyTests
{
    [Fact]
    public void Multiply_WithNoDecimalsCurrencyAndHalfEvenValue_ShouldRoundToEven()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var clp = Currency.FromCode("CLP"); // 0 decimals
        var money = new Money(10.05m, usd);

        // Act
        // 10.05 * 850.5 = 8547.525. Banker's Rounding to 0 decimals should round 8547.525 to 8548.
        var result = money.Multiply(850.5m, clp);

        // Assert
        result.Amount.Should().Be(8548m);
        result.Currency.Code.Should().Be("CLP");
    }

    [Fact]
    public void Multiply_WithTwoDecimalsCurrencyAndHalfValue_ShouldRoundToEven()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var money = new Money(1.00m, usd);

        // Act
        // 1.00m * 1.005m = 1.005m. Banker's Rounding to 2 decimals should round 1.005m to 1.00m (even).
        var result = money.Multiply(1.005m, usd);

        // Assert
        result.Amount.Should().Be(1.00m);
    }

    [Fact]
    public void Multiply_WithTwoDecimalsCurrencyAndHalfValueRoundingUp_ShouldRoundToEven()
    {
        // Arrange
        var usd = Currency.FromCode("USD");
        var money = new Money(1.00m, usd);

        // Act
        // 1.00m * 1.015m = 1.015m. Banker's Rounding to 2 decimals should round 1.015m to 1.02m (even).
        var result = money.Multiply(1.015m, usd);

        // Assert
        result.Amount.Should().Be(1.02m);
    }
}
