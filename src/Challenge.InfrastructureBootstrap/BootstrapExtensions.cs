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
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration);
        services.AddControllers();
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
        try
        {
            await DbMigrator.MigrateAndSeedAsync(app.Services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during database migration: {ex.Message}");
        }

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

        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapControllers();
    }
}
