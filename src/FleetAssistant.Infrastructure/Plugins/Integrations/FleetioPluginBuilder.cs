using FleetAssistant.Infrastructure.Configuration;
using FleetAssistant.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FleetAssistant.Infrastructure.Plugins.Integrations;

/// <summary>
/// Fleetio integration plugin builder
/// </summary>
public class FleetioPluginBuilder : IIntegrationPluginBuilder
{
    private readonly ICredentialStore _credentialStore;
    private readonly IIntegrationConfigStore _configStore;
    private readonly ILogger<FleetioPluginBuilder> _logger;

    public string Key => "fleetio";
    public string DisplayName => "Fleetio";
    public IReadOnlyList<string> Capabilities => new[] { "maintenance", "fuel", "vehicle-data", "work-orders" }.AsReadOnly();

    public FleetioPluginBuilder(
        ICredentialStore credentialStore,
        IIntegrationConfigStore configStore,
        ILogger<FleetioPluginBuilder> logger)
    {
        _credentialStore = credentialStore;
        _configStore = configStore;
        _logger = logger;
    }

    public async Task<KernelPlugin?> BuildPluginAsync(string tenantId)
    {
        try
        {
            if (!await IsAvailableForTenantAsync(tenantId))
            {
                _logger.LogInformation("Fleetio integration not available for tenant {TenantId}", tenantId);
                return null;
            }

            var credentials = await _credentialStore.GetCredentialsAsync(tenantId, Key);
            var config = await _configStore.GetIntegrationConfigAsync(tenantId, Key);

            if (credentials == null || config == null)
            {
                _logger.LogWarning("Missing credentials or config for Fleetio integration for tenant {TenantId}", tenantId);
                return null;
            }

            var plugin = KernelPluginFactory.CreateFromObject(
                new FleetioPlugin(credentials, config, _logger),
                pluginName: Key);

            _logger.LogInformation("Successfully created Fleetio plugin for tenant {TenantId}", tenantId);
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build Fleetio plugin for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> IsAvailableForTenantAsync(string tenantId)
    {
        return await _configStore.IsIntegrationEnabledAsync(tenantId, Key);
    }
}

/// <summary>
/// Fleetio plugin implementation
/// </summary>
public class FleetioPlugin
{
    private readonly IReadOnlyDictionary<string, string> _credentials;
    private readonly IReadOnlyDictionary<string, string> _config;
    private readonly ILogger _logger;

    public FleetioPlugin(
        IReadOnlyDictionary<string, string> credentials,
        IReadOnlyDictionary<string, string> config,
        ILogger logger)
    {
        _credentials = credentials;
        _config = config;
        _logger = logger;
    }

    [KernelFunction, Description("Gets maintenance work orders for a vehicle from Fleetio")]
    public async Task<string> GetWorkOrdersAsync(
        [Description("Vehicle ID or VIN")] string vehicleId,
        [Description("Status filter: open, closed, all")] string status = "all")
    {
        try
        {
            _logger.LogInformation("Getting work orders from Fleetio for vehicle {VehicleId}", vehicleId);            await Task.Delay(100); // Simulate API call

            var mockData = new
            {
                vehicleId = vehicleId,
                workOrders = new object[]
                {
                    new 
                    { 
                        id = "WO-12345",
                        status = "Open",
                        type = "Preventive Maintenance",
                        description = "5,000 mile service",
                        priority = "Medium",
                        dueDate = "2024-02-20",
                        estimatedCost = "$285.00"
                    },
                    new 
                    { 
                        id = "WO-12340",
                        status = "Closed",
                        type = "Repair",
                        description = "Replace brake pads",
                        completedDate = "2024-01-15",
                        actualCost = "$425.50"
                    }
                },
                totalOpenWorkOrders = 1,
                totalEstimatedCost = "$285.00",
                source = "Fleetio"
            };

            return System.Text.Json.JsonSerializer.Serialize(mockData, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get work orders from Fleetio for vehicle {VehicleId}", vehicleId);
            return $"Error retrieving work orders: {ex.Message}";
        }
    }

    [KernelFunction, Description("Gets fuel transactions and analysis from Fleetio")]
    public async Task<string> GetFuelTransactionsAsync(
        [Description("Vehicle ID or VIN")] string vehicleId,
        [Description("Start date in ISO format")] string startDate = "",
        [Description("End date in ISO format")] string endDate = "")
    {
        try
        {
            _logger.LogInformation("Getting fuel transactions from Fleetio for vehicle {VehicleId}", vehicleId);

            await Task.Delay(100); // Simulate API call

            var mockData = new
            {
                vehicleId = vehicleId,
                timeframe = $"{startDate} to {endDate}",
                transactions = new object[]
                {
                    new 
                    {
                        date = "2024-01-25",
                        gallons = 18.5,
                        cost = "$67.15",
                        pricePerGallon = "$3.63",
                        location = "Shell Station - Main St",
                        odometer = 74850
                    },
                    new 
                    {
                        date = "2024-01-18",
                        gallons = 20.2,
                        cost = "$73.12",
                        pricePerGallon = "$3.62",
                        location = "BP Station - Highway 95",
                        odometer = 74620
                    }
                },
                summary = new
                {
                    totalGallons = 38.7,
                    totalCost = "$140.27",
                    averageMpg = "7.1 MPG",
                    averagePricePerGallon = "$3.63"
                },
                source = "Fleetio"
            };

            return System.Text.Json.JsonSerializer.Serialize(mockData, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fuel transactions from Fleetio for vehicle {VehicleId}", vehicleId);
            return $"Error retrieving fuel transactions: {ex.Message}";
        }
    }

    [KernelFunction, Description("Gets vehicle asset information from Fleetio")]
    public async Task<string> GetVehicleAssetAsync(
        [Description("Vehicle ID or VIN")] string vehicleId)
    {
        try
        {
            _logger.LogInformation("Getting vehicle asset from Fleetio for vehicle {VehicleId}", vehicleId);

            await Task.Delay(100); // Simulate API call

            var mockData = new
            {
                vehicleId = vehicleId,
                asset = new
                {
                    id = 12345,
                    name = "Fleet Vehicle #42",
                    vin = "1HGBH41JXMN109186",
                    year = 2023,
                    make = "Ford",
                    model = "Transit 250",
                    trim = "Base",
                    licensePlate = "ABC123",
                    status = "Active",
                    mileage = 74892,
                    acquisitionDate = "2023-03-15",
                    acquisitionCost = "$45,250.00",
                    currentValue = "$38,750.00",
                    department = "Delivery",
                    assignedDriver = "John Smith"
                },
                source = "Fleetio"
            };

            return System.Text.Json.JsonSerializer.Serialize(mockData, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vehicle asset from Fleetio for vehicle {VehicleId}", vehicleId);
            return $"Error retrieving vehicle asset: {ex.Message}";
        }
    }
}
