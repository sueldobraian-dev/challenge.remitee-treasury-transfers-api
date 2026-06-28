using Challenge.Application.Common.DispatchR;

namespace Challenge.Application.Features.Transfers.Commands;

public record CreateTransferCommand(
    Guid OperationId,
    string SourceAccountId,
    string TargetAccountId,
    decimal Amount,
    string Currency,
    decimal? Fx
) : IRequest<TransferResultResponse>;
