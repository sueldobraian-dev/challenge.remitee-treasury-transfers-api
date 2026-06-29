using FluentValidation;

namespace Challenge.Application.Features.Transfers.Commands;

public class CreateTransferCommandValidator : AbstractValidator<CreateTransferCommand>
{
    public CreateTransferCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("The transfer amount must be strictly positive.")
            .WithErrorCode("INVALID_AMOUNT");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.SourceAccountId, x.TargetAccountId, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source and target accounts must be different.")
            .WithErrorCode("IDENTICAL_ACCOUNTS")
            .WithName("Accounts");
    }
}
