using Challenge.Application.Common.DispatchR;
using Microsoft.Extensions.DependencyInjection;

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

        // Obtener todos los pipeline behaviors registrados para la petición/respuesta
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = _serviceProvider.GetServices(behaviorType);

        RequestHandlerDelegate<TResponse> next = () =>
        {
            var method = handlerType.GetMethod("HandleAsync");
            if (method == null)
            {
                throw new InvalidOperationException($"HandleAsync method not found on handler type {handlerType.Name}.");
            }
            return (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;
        };

        // Encadenar los behaviors en orden inverso para su correcta ejecución
        foreach (var behavior in behaviors.Cast<dynamic>().Reverse())
        {
            var currentNext = next;
            next = () => behavior.HandleAsync((dynamic)request, currentNext, cancellationToken);
        }

        return await next();
    }
}
