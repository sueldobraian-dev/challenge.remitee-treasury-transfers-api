using System.Text.Json;
using Asp.Versioning;
using Challenge.Infrastructure.Persistence;
using Challenge.InfrastructureBootstrap.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        // 4. Configure API Versioning (query string based: ?api-version=1.0)
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = false;
        });

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
        // 1. Run automatic DB migrations and seed initial accounts
        try
        {
            await DbMigrator.MigrateAndSeedAsync(app.Services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during database migration: {ex.Message}");
        }

        // 2. Validate API Version query parameter (only support 1.0 / 1)
        app.Use(async (context, next) =>
        {
            if (context.Request.Query.TryGetValue("api-version", out var versionValues))
            {
                var version = versionValues.ToString();
                if (version != "1.0" && version != "1")
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest;

                    var error = new Challenge.Domain.Errors.Error(
                        System.Net.HttpStatusCode.BadRequest,
                        "UNSUPPORTED_API_VERSION",
                        "The requested API version is not supported.",
                        $"The API version '{version}' is not supported. Supported versions are: 1.0"
                    );

                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(error, options));
                    return;
                }
            }
            await next();
        });

        // 3. Enable native OpenAPI JSON endpoint
        app.MapOpenApi();

        // 4. Map Scalar interactive API reference UI (accessible at /scalar/v1)
        app.MapScalarApiReference();

        // 5. Map Controller endpoints
        app.MapControllers();
    }
}
