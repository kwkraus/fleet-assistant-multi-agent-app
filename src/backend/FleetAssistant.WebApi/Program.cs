using FleetAssistant.Shared.Services;
using FleetAssistant.WebApi.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fleet Assistant API",
        Version = "v1",
        Description = "ASP.NET Core WebAPI for Fleet Assistant with Azure AI Foundry Agent Service integration",
        Contact = new OpenApiContact
        {
            Name = "Fleet Assistant Team"
        }
    });

    // Set the comments path for the Swagger JSON and UI
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add support for Server-Sent Events
    c.MapType<IAsyncEnumerable<string>>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "text/event-stream",
        Description = "Server-Sent Events stream"
    });
});

// Register HTTP client (needed for AgentServiceClient fallback)
builder.Services.AddHttpClient();

// Register agent service client based on configuration
var useFoundryAgent = builder.Configuration.GetValue<bool>("UseFoundryAgent", true);

if (useFoundryAgent)
{
    // FoundryAgentService uses Azure.AI.Agents.Persistent with DefaultAzureCredential
    builder.Services.AddSingleton<IAgentServiceClient, FoundryAgentService>();
}
else
{
    // Fallback to mock AgentServiceClient - you'll need to implement this
    // builder.Services.AddScoped<IAgentServiceClient, AgentServiceClient>();
    throw new InvalidOperationException("Mock AgentServiceClient not implemented yet. Set UseFoundryAgent to true.");
}

// Add CORS policy for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fleet Assistant API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
