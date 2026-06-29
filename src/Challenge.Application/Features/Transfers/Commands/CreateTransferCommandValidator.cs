using FluentValidation;

namespace Challenge.Application.Features.Transfers.Commands;

public class CreateTransferCommandValidator : AbstractValidator<CreateTransferCommand>
{
    public CreateTransferCommandValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty()
            .WithMessage("Operation ID is required.")
            .WithErrorCode("EMPTY_OPERATION_ID");

        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .WithMessage("Source account ID is required.")
            .WithErrorCode("EMPTY_SOURCE_ACCOUNT");

        RuleFor(x => x.TargetAccountId)
            .NotEmpty()
            .WithMessage("Target account ID is required.")
            .WithErrorCode("EMPTY_TARGET_ACCOUNT");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("The transfer amount must be strictly positive.")
            .WithErrorCode("INVALID_AMOUNT");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .WithErrorCode("EMPTY_CURRENCY");

        RuleFor(x => x)
            .Must(x => string.IsNullOrEmpty(x.SourceAccountId) || 
                       string.IsNullOrEmpty(x.TargetAccountId) || 
                       !string.Equals(x.SourceAccountId, x.TargetAccountId, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source and target accounts must be different.")
            .WithErrorCode("IDENTICAL_ACCOUNTS")
            .WithName("Accounts");
    }
}
