using FleetAssistant.Api.Services;
using FleetAssistant.Agents;
using FleetAssistant.Infrastructure.Configuration;
using FleetAssistant.Infrastructure.Security;
using FleetAssistant.Infrastructure.Plugins;
using FleetAssistant.Infrastructure.Plugins.Integrations;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add services
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register Semantic Kernel
builder.Services.AddTransient<IKernelBuilder>(_ => Kernel.CreateBuilder());

// Register Infrastructure services
builder.Services.AddSingleton<IIntegrationConfigStore, InMemoryIntegrationConfigStore>();
builder.Services.AddSingleton<ICredentialStore, InMemoryCredentialStore>();

// Register Integration Plugin Builders
builder.Services.AddScoped<IIntegrationPluginBuilder, GeoTabPluginBuilder>();
builder.Services.AddScoped<IIntegrationPluginBuilder, FleetioPluginBuilder>();
builder.Services.AddScoped<IIntegrationPluginBuilder, SamsaraPluginBuilder>();

// Register Plugin Registry
builder.Services.AddScoped<IIntegrationPluginRegistry, IntegrationPluginRegistry>();

// Register Agents
builder.Services.AddScoped<FuelAgent>();
builder.Services.AddScoped<MaintenanceAgent>();
builder.Services.AddScoped<SafetyAgent>();
builder.Services.AddScoped<PlanningAgent>();

// Register custom services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

builder.Build().Run();
