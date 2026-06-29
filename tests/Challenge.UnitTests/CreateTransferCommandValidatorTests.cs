using System;
using Challenge.Application.Features.Transfers.Commands;
using FluentAssertions;
using Xunit;

namespace Challenge.UnitTests;

public class CreateTransferCommandValidatorTests
{
    private readonly CreateTransferCommandValidator _validator;

    public CreateTransferCommandValidatorTests()
    {
        _validator = new CreateTransferCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldBeValid()
    {
        // Arrange
        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-50.25)]
    public void Validate_WithInvalidAmount_ShouldHaveValidationErrorForAmount(decimal amount)
    {
        // Arrange
        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: amount,
            Currency: "USD",
            Fx: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.PropertyName.Should().Be(nameof(CreateTransferCommand.Amount));
        error.ErrorCode.Should().Be("INVALID_AMOUNT");
        error.ErrorMessage.Should().Be("The transfer amount must be strictly positive.");
    }

    [Theory]
    [InlineData("ACC-USD-1", "ACC-USD-1")]
    [InlineData("ACC-USD-1", "acc-usd-1")]
    public void Validate_WithIdenticalAccounts_ShouldHaveValidationErrorForAccounts(string source, string target)
    {
        // Arrange
        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: source,
            TargetAccountId: target,
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.PropertyName.Should().Be("Accounts");
        error.ErrorCode.Should().Be("IDENTICAL_ACCOUNTS");
        error.ErrorMessage.Should().Be("Source and target accounts must be different.");
    }

    [Fact]
    public void Validate_WithEmptyOperationId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateTransferCommand(
            OperationId: Guid.Empty,
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.PropertyName.Should().Be(nameof(CreateTransferCommand.OperationId));
        error.ErrorCode.Should().Be("EMPTY_OPERATION_ID");
        error.ErrorMessage.Should().Be("Operation ID is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptySourceAccountId_ShouldHaveValidationError(string? sourceAccountId)
    {
        // Arrange
        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: sourceAccountId!,
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.PropertyName.Should().Be(nameof(CreateTransferCommand.SourceAccountId));
        error.ErrorCode.Should().Be("EMPTY_SOURCE_ACCOUNT");
        error.ErrorMessage.Should().Be("Source account ID is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyTargetAccountId_ShouldHaveValidationError(string? targetAccountId)
    {
        // Arrange
        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: targetAccountId!,
            Amount: 100.00m,
            Currency: "USD",
            Fx: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.PropertyName.Should().Be(nameof(CreateTransferCommand.TargetAccountId));
        error.ErrorCode.Should().Be("EMPTY_TARGET_ACCOUNT");
        error.ErrorMessage.Should().Be("Target account ID is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyCurrency_ShouldHaveValidationError(string? currency)
    {
        // Arrange
        var command = new CreateTransferCommand(
            OperationId: Guid.NewGuid(),
            SourceAccountId: "ACC-USD-1",
            TargetAccountId: "ACC-USD-2",
            Amount: 100.00m,
            Currency: currency!,
            Fx: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.PropertyName.Should().Be(nameof(CreateTransferCommand.Currency));
        error.ErrorCode.Should().Be("EMPTY_CURRENCY");
        error.ErrorMessage.Should().Be("Currency is required.");
    }
}
