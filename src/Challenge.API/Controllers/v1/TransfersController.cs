using Asp.Versioning;
using Challenge.Application.Features.Transfers.Commands;
using Challenge.Application.Features.Transfers.Commands.Responses;
using Challenge.InfrastructureBootstrap.Integrations.DispatchR;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.API.Controllers.v1;

/// <summary>
/// Controller handling treasury transfer operations between accounts.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("transfers")]
public class TransfersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public TransfersController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Creates and processes a new internal treasury transfer between two accounts.
    /// </summary>
    /// <param name="command">The details of the transfer request, including the unique operation ID, source/target accounts, currency, and amount.</param>
    /// <returns>A summary of the processed transfer including transaction status and IDs.</returns>
    /// <response code="200">The transfer was processed successfully.</response>
    /// <response code="400">If the request payload fails validation, currencies mismatch, or the transfer constraints are violated (e.g. frozen accounts, insufficient funds).</response>
    /// <response code="404">If the source or target account is not found.</response>
    /// <response code="409">If the operation ID already exists (idempotency conflict).</response>
    /// <response code="500">If an unexpected internal server error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransferResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<TransferResultResponse> CreateAsync([FromBody] CreateTransferCommand command)
        => await _dispatcher.SendAsync(command);
}
