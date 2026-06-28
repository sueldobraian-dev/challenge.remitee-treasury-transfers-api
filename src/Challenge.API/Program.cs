using Microsoft.AspNetCore.Builder;
using Challenge.InfrastructureBootstrap;

var builder = WebApplication.CreateBuilder(args);

// Bootstrap configurations and DI registration
builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

// Bootstrap middleware pipeline, migrations, and endpoint mapping
await app.ConfigureAppPipelineAsync();

app.Run();
