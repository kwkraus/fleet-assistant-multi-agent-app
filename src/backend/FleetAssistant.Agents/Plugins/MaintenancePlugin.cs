using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FleetAssistant.Agents.Plugins;

/// <summary>
/// Plugin for maintenance management operations
/// </summary>
public sealed class MaintenancePlugin
{
    [KernelFunction]
    [Description("Check maintenance schedules and upcoming service requirements")]
    public string CheckMaintenanceSchedule(string vehicleId = "all")
    {
        if (vehicleId == "all")
        {
            return "Upcoming maintenance: V001 - Oil change due in 500 miles, " +
                   "V002 - Inspection due next week, V003 - Tire rotation overdue, " +
                   "V004 - No maintenance required.";
        }
        
        return $"Vehicle {vehicleId}: Oil change due in 800 miles, next inspection in 3 months.";
    }

    [KernelFunction]
    [Description("Get vehicle health status and diagnostic information")]
    public string GetVehicleHealth(string vehicleId)
    {
        return $"Vehicle {vehicleId} health status: Engine: Good, Brakes: Excellent, " +
               "Transmission: Good, Battery: 82%, Tires: Fair (replace within 2 months). " +
               "Overall score: 8.5/10";
    }

    [KernelFunction]
    [Description("Schedule maintenance appointments and service")]
    public string ScheduleMaintenance(string vehicleId, string serviceType, string preferredDate)
    {
        return $"Maintenance scheduled for vehicle {vehicleId}: {serviceType} on {preferredDate}. " +
               "Service provider: AutoCare Plus, Estimated duration: 2 hours, Cost: $150-200.";
    }

    [KernelFunction]
    [Description("Get maintenance cost analysis and budget tracking")]
    public string GetMaintenanceCosts(string period = "quarter")
    {
        return $"Maintenance costs for {period}: $12,300 (15% under budget). " +
               "Major expenses: Tire replacements $4,200, Oil changes $2,100, Inspections $1,800.";
    }

    [KernelFunction]
    [Description("Get maintenance history and records for a vehicle")]
    public string GetMaintenanceHistory(string vehicleId)
    {
        return $"Vehicle {vehicleId} maintenance history: Last oil change: 2 weeks ago, " +
               "Last inspection: 3 months ago (passed), Tire rotation: 1 month ago, " +
               "Brake service: 6 months ago. Total maintenance cost YTD: $850.";
    }
}
