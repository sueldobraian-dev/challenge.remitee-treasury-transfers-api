using Challenge.InfrastructureBootstrap;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

await app.ConfigureAppPipelineAsync();

app.Run();
