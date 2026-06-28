using System.Reflection;
using Challenge.Application.Common.DispatchR;
using Microsoft.Extensions.DependencyInjection;

namespace Challenge.InfrastructureBootstrap.Integrations.DispatchR;

public static class DispatchRExtensions
{
    public static IServiceCollection AddDispatchR(this IServiceCollection services, Assembly assembly)
    {
        services.AddSingleton<IDispatcher, Dispatcher>();

        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces(), (implementation, iface) => new { Implementation = implementation, Interface = iface })
            .Where(x => x.Interface.IsGenericType && x.Interface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

        foreach (var handler in handlerTypes)
        {
            services.AddTransient(handler.Interface, handler.Implementation);
        }

        return services;
    }
}
