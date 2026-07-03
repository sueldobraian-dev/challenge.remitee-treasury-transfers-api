using System;

namespace Challenge.Application.Common.Events;

public record TransferStartedEvent(
    Guid TransactionId,
    Guid OperationId,
    string SourceAccountId,
    string TargetAccountId,
    decimal AmountDebited,
    decimal AmountCredited,
    decimal? FxRate
);

public record SourceAccountDebitedEvent(
    Guid TransactionId,
    Guid OperationId,
    string SourceAccountId,
    decimal AmountDebited
);

public record TransferCompletedEvent(
    Guid TransactionId,
    Guid OperationId,
    string SourceAccountId,
    string TargetAccountId,
    decimal AmountDebited,
    decimal AmountCredited
);

public record TransferFailedEvent(
    Guid TransactionId,
    Guid OperationId,
    string Reason
);
