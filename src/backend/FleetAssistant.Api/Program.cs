using FleetAssistant.Api.Services;
using FleetAssistant.Shared.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add services
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register HTTP client
builder.Services.AddHttpClient();

// Register agent service client based on configuration
var useFoundryAgent = builder.Configuration.GetValue<bool>("UseFoundryAgent", true);

if (useFoundryAgent)
{
    builder.Services.AddScoped<IAgentServiceClient, FoundryAgentService>();
}
else
{
    builder.Services.AddScoped<IAgentServiceClient, AgentServiceClient>();
}

builder.Build().Run();

// Make Program class accessible for testing
public partial class Program { }
