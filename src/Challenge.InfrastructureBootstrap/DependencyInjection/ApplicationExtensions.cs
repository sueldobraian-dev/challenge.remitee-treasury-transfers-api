using Challenge.Application.Features.Transfers.Commands;
using Challenge.InfrastructureBootstrap.Integrations.DispatchR;
using Microsoft.Extensions.DependencyInjection;

namespace Challenge.InfrastructureBootstrap.DependencyInjection;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddDispatchR(typeof(CreateTransferCommandHandler).Assembly);
        return services;
    }
}
