using FleetAssistant.Shared.Services;
using FleetAssistant.WebApi.Data;
using FleetAssistant.WebApi.Options;
using FleetAssistant.WebApi.Repositories;
using FleetAssistant.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    // Use in-memory database for development/testing if no connection string is provided
    builder.Services.AddDbContext<FleetAssistantDbContext>(options =>
        options.UseInMemoryDatabase("FleetAssistantInMemoryDb"));
}
else
{
    builder.Services.AddDbContext<FleetAssistantDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IFuelLogRepository, FuelLogRepository>();
builder.Services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();
builder.Services.AddScoped<IInsuranceRepository, InsuranceRepository>();
builder.Services.AddScoped<IFinancialRepository, FinancialRepository>();

// Configure Blob Storage
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName));

// Register Blob Storage services
builder.Services.AddSingleton(provider =>
{
    var blobStorageOptions = builder.Configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>();
    return new BlobServiceClient(blobStorageOptions?.ConnectionString ?? "UseDevelopmentStorage=true");
});
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

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

// Configure FoundryAgentOptions
builder.Services.Configure<FoundryAgentOptions>(
    builder.Configuration.GetSection(FoundryAgentOptions.SectionName));

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

// Add CORS policy for frontend (HTTP/2+ compatible)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Type", "Cache-Control"); // Expose SSE headers
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
