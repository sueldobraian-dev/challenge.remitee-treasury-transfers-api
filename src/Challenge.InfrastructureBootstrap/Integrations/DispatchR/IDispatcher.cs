using Challenge.Application.Common.DispatchR;

namespace Challenge.InfrastructureBootstrap.Integrations.DispatchR;

public interface IDispatcher
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
