using Challenge.Application.Common.DispatchR;

namespace Challenge.InfrastructureBootstrap.Integrations.DispatchR;

public class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler of type {handlerType.Name} was registered for request {requestType.Name}.");
        }

        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
        {
            throw new InvalidOperationException($"HandleAsync method not found on handler type {handlerType.Name}.");
        }

        var resultTask = (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;
        return await resultTask;
    }
}
