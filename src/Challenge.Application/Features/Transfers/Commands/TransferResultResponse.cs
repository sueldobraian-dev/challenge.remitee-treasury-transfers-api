namespace Challenge.Application.Features.Transfers.Commands;

public record TransferResultResponse(
    Guid Id,
    Guid OperationId,
    string Status,
    string SourceAccountId,
    string TargetAccountId,
    decimal AmountDebited,
    decimal AmountCredited,
    DateTimeOffset CreatedAt
);
