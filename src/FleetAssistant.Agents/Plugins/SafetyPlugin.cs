using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FleetAssistant.Agents.Plugins;

/// <summary>
/// Plugin for safety and compliance operations
/// </summary>
public sealed class SafetyPlugin
{
    [KernelFunction]
    [Description("Check driver safety scores and performance metrics")]
    public string CheckDriverSafety(string driverId = "all")
    {
        if (driverId == "all")
        {
            return "Driver safety scores: D001: 95% (Excellent), D002: 87% (Good), " +
                   "D003: 78% (Needs improvement), D004: 92% (Excellent). " +
                   "Fleet average: 88% - Above industry standard.";
        }
        
        return $"Driver {driverId} safety score: 89% (Good). Recent incidents: 0, " +
               "Speed violations: 2 (minor), Defensive driving course completed.";
    }

    [KernelFunction]
    [Description("Get vehicle safety inspection status and compliance")]
    public string GetSafetyInspections(string vehicleId = "all")
    {
        if (vehicleId == "all")
        {
            return "Safety inspection status: V001: Current (expires 6/2025), " +
                   "V002: Current (expires 8/2025), V003: Expires next month - schedule renewal, " +
                   "V004: Current (expires 12/2025).";
        }
        
        return $"Vehicle {vehicleId} safety inspection: Current, expires in 4 months. " +
               "Last inspection: All systems passed. Next due: October 2025.";
    }

    [KernelFunction]
    [Description("Report and track safety incidents")]
    public string ReportSafetyIncident(string incidentType, string description, string vehicleId)
    {
        return $"Safety incident reported: {incidentType} involving vehicle {vehicleId}. " +
               $"Description: {description}. Incident ID: SI-2025-0124. " +
               "Investigation assigned to Safety Officer. Driver training scheduled.";
    }

    [KernelFunction]
    [Description("Get compliance status for regulations and certifications")]
    public string GetComplianceStatus(string regulationType = "all")
    {
        return $"Compliance status for {regulationType}: DOT certifications: Current, " +
               "Driver licenses: All valid, Vehicle registrations: Current, " +
               "Insurance: Active and compliant. Last audit: Passed with minor recommendations.";
    }

    [KernelFunction]
    [Description("Schedule safety training for drivers")]
    public string ScheduleSafetyTraining(string driverId, string trainingType)
    {
        return $"Safety training scheduled for driver {driverId}: {trainingType}. " +
               "Date: Next available session - March 15th, Duration: 4 hours, " +
               "Location: Training Center Downtown. Certification upon completion.";
    }
}
