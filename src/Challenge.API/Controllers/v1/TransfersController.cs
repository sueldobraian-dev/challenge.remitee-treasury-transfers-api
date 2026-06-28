using Challenge.Application.Features.Transfers.Commands;
using Challenge.InfrastructureBootstrap.Integrations.DispatchR;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.API.Controllers.v1;

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
