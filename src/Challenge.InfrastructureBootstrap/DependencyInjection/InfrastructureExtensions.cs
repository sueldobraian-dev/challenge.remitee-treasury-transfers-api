using Challenge.Application.Common.Interfaces;
using Challenge.Application.Features.Transfers.Orchestration;
using Challenge.Domain.Repositories;
using Challenge.Infrastructure.Persistence;
using Challenge.Infrastructure.Persistence.Repositories;
using Challenge.Infrastructure.Services;
using Challenge.InfrastructureBootstrap.Integrations.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Challenge.InfrastructureBootstrap.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in the configuration.");
        }

        services.AddDbContext<ChallengeDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUnitOfWork>(provider =>
        {
            var dbContext = provider.GetRequiredService<ChallengeDbContext>();
            var logger = provider.GetRequiredService<ILogger<Challenge.InfrastructureBootstrap.Integrations.Persistence.RetryUnitOfWorkDecorator>>();
            return new RetryUnitOfWorkDecorator(dbContext, logger);
        });

        services.AddScoped<IKafkaService, KafkaService>();
        services.AddScoped<ITransferSagaOrchestrator, TransferSagaOrchestrator>();

        return services;
    }
}
