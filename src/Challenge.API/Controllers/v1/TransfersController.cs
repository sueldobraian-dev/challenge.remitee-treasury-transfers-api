using Asp.Versioning;
using Challenge.Application.Features.Transfers.Commands;
using Challenge.Application.Features.Transfers.Commands.Responses;
using Challenge.InfrastructureBootstrap.Integrations.DispatchR;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.API.Controllers.v1;

/// <summary>
/// API para operaciones de tesorería (transferencias internas).
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

    [HttpPost]
    public async Task<TransferResultResponse> CreateAsync([FromBody] CreateTransferCommand command)
        => await _dispatcher.SendAsync(command);
}
