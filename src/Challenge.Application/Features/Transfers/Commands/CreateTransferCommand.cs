using System;
using Challenge.Application.Common.DispatchR;

namespace Challenge.Application.Features.Transfers.Commands;

/// <summary>
/// Command to request a new internal treasury transfer.
/// </summary>
/// <param name="OperationId">Unique identifier (Idempotency Key) to prevent duplicate processing. Example: "a3f1c9d2-1111-2222-3333-444444444444"</param>
/// <param name="SourceAccountId">The ID of the originating account. Example: "ACC-USD-1"</param>
/// <param name="TargetAccountId">The ID of the receiving account. Example: "ACC-ARS-1"</param>
/// <param name="Amount">The positive amount of money to transfer. Example: 100.00</param>
/// <param name="Currency">The ISO currency code of the transfer (must match the source account currency). Example: "USD"</param>
/// <param name="Fx">The optional exchange rate. Mandatory if currencies differ. Example: 1000.00</param>
public record CreateTransferCommand(
    Guid OperationId,
    string SourceAccountId,
    string TargetAccountId,
    decimal Amount,
    string Currency,
    decimal? Fx
) : IRequest<TransferResultResponse>;
