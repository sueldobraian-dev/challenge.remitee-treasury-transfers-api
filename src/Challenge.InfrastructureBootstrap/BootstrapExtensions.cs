using Challenge.Infrastructure.Persistence;
using Challenge.InfrastructureBootstrap.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Challenge.InfrastructureBootstrap;

public static class BootstrapExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Register Application services (scanning command handlers for DispatchR)
        services.AddApplicationServices();

        // 2. Register Infrastructure services (DbContext, repositories, Unit of Work)
        services.AddInfrastructureServices(configuration);

        // 3. Register Controllers support
        services.AddControllers();

        // 4. Configure native OpenAPI (.NET 10 style) with custom metadata
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "Treasury Transfers API";
                document.Info.Version = "v1";
                document.Info.Description = "API for handling atomic and idempotent internal treasury transfers.";
                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static async Task ConfigureAppPipelineAsync(this WebApplication app)
    {
        // Register exception handling middleware at the beginning of the pipeline
        app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();

        // 1. Run automatic DB migrations and seed initial accounts
        try
        {
            await DbMigrator.MigrateAndSeedAsync(app.Services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during database migration: {ex.Message}");
        }

        // 2. Enable native OpenAPI JSON endpoint
        app.MapOpenApi();

        // 3. Map Scalar interactive API reference UI (accessible at /scalar/v1)
        app.MapScalarApiReference();

        // 4. Map Controller endpoints
        app.MapControllers();
    }
}
