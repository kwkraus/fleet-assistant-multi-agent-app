using FleetAssistant.Infrastructure.Configuration;
using FleetAssistant.Infrastructure.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add services
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register Infrastructure services
builder.Services.AddSingleton<IIntegrationConfigStore, InMemoryIntegrationConfigStore>();
builder.Services.AddSingleton<ICredentialStore, InMemoryCredentialStore>();

builder.Build().Run();
