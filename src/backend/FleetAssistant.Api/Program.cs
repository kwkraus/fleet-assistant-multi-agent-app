using FleetAssistant.Api.Services;
using FleetAssistant.Shared.Services;
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

// Register HTTP client and AgentServiceClient
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAgentServiceClient, AgentServiceClient>();

builder.Build().Run();

// Make Program class accessible for testing
public partial class Program { }
