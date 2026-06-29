using System.Reflection;
using Challenge.Application.Common.DispatchR;
using Challenge.InfrastructureBootstrap.Integrations.DispatchR;
using Challenge.InfrastructureBootstrap.Integrations.DispatchR.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Challenge.InfrastructureBootstrap.Integrations.DispatchR;

public static class DispatchRExtensions
{
    public static IServiceCollection AddDispatchR(this IServiceCollection services, Assembly assembly)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddValidatorsFromAssembly(assembly);

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
