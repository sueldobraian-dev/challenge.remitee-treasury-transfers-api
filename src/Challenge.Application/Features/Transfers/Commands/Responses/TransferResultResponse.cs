using System;

namespace Challenge.Application.Features.Transfers.Commands.Responses;

/// <summary>
/// Response payload for a successful transfer.
/// </summary>
/// <param name="Id">The unique database identifier of the transaction. Example: "8fa8d39c-5555-6666-7777-888888888888"</param>
/// <param name="OperationId">The idempotency key associated with this operation. Example: "a3f1c9d2-1111-2222-3333-444444444444"</param>
/// <param name="Status">The final status of the transaction. Example: "COMPLETED"</param>
/// <param name="SourceAccountId">The ID of the originating account. Example: "ACC-USD-1"</param>
/// <param name="TargetAccountId">The ID of the receiving account. Example: "ACC-ARS-1"</param>
/// <param name="AmountDebited">The exact amount debited from the source account. Example: 100.00</param>
/// <param name="AmountCredited">The exact rounded amount credited to the target account. Example: 100000.00</param>
/// <param name="CreatedAt">The timestamp when the transfer was executed. Example: "2026-05-28T13:00:00.000Z"</param>
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
