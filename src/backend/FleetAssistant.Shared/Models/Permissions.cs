namespace FleetAssistant.Shared.Models;

/// <summary>
/// Represents a permission that can be granted to API keys or roles
/// </summary>
public class Permission
{
    /// <summary>
    /// Unique permission identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable permission name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this permission allows
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category of permission (Agent, Integration, Admin, etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a dangerous permission requiring extra verification
    /// </summary>
    public bool RequiresElevation { get; set; } = false;
}

/// <summary>
/// Standard permissions available in the system
/// </summary>
public static class Permissions
{
    // Fleet Query Permissions
    public static readonly Permission FleetQuery = new()
    {
        Id = "fleet:query",
        Name = "Fleet Query",
        Description = "Can query fleet information using AI agents",
        Category = "Fleet"
    };

    public static readonly Permission FleetQueryAdvanced = new()
    {
        Id = "fleet:query:advanced",
        Name = "Advanced Fleet Query",
        Description = "Can use advanced AI models and complex queries",
        Category = "Fleet",
        RequiresElevation = true
    };

    // Agent Permissions
    public static readonly Permission AgentFuel = new()
    {
        Id = "agent:fuel",
        Name = "Fuel Agent Access",
        Description = "Can access fuel efficiency and cost data",
        Category = "Agent"
    };

    public static readonly Permission AgentMaintenance = new()
    {
        Id = "agent:maintenance",
        Name = "Maintenance Agent Access",
        Description = "Can access maintenance and work order data",
        Category = "Agent"
    };

    public static readonly Permission AgentSafety = new()
    {
        Id = "agent:safety",
        Name = "Safety Agent Access",
        Description = "Can access safety events and compliance data",
        Category = "Agent"
    };

    public static readonly Permission AgentLocation = new()
    {
        Id = "agent:location",
        Name = "Location Agent Access",
        Description = "Can access vehicle location and routing data",
        Category = "Agent"
    };

    public static readonly Permission AgentCompliance = new()
    {
        Id = "agent:compliance",
        Name = "Compliance Agent Access",
        Description = "Can access regulatory compliance data",
        Category = "Agent"
    };

    public static readonly Permission AgentFinancial = new()
    {
        Id = "agent:financial",
        Name = "Financial Agent Access",
        Description = "Can access financial and cost analysis data",
        Category = "Agent"
    };

    // Integration Permissions
    public static readonly Permission IntegrationGeotab = new()
    {
        Id = "integration:geotab",
        Name = "GeoTab Integration",
        Description = "Can access GeoTab fleet management data",
        Category = "Integration"
    };

    public static readonly Permission IntegrationFleetio = new()
    {
        Id = "integration:fleetio",
        Name = "Fleetio Integration",
        Description = "Can access Fleetio fleet management data",
        Category = "Integration"
    };

    public static readonly Permission IntegrationSamsara = new()
    {
        Id = "integration:samsara",
        Name = "Samsara Integration",
        Description = "Can access Samsara fleet management data",
        Category = "Integration"
    };

    // Data Access Permissions
    public static readonly Permission DataRealTime = new()
    {
        Id = "data:realtime",
        Name = "Real-time Data Access",
        Description = "Can access real-time fleet data",
        Category = "Data"
    };

    public static readonly Permission DataHistorical = new()
    {
        Id = "data:historical",
        Name = "Historical Data Access",
        Description = "Can access historical fleet data (last 30 days)",
        Category = "Data"
    };

    public static readonly Permission DataHistoricalExtended = new()
    {
        Id = "data:historical:extended",
        Name = "Extended Historical Data Access",
        Description = "Can access historical fleet data beyond 30 days",
        Category = "Data",
        RequiresElevation = true
    };

    public static readonly Permission DataExport = new()
    {
        Id = "data:export",
        Name = "Data Export",
        Description = "Can export data in various formats",
        Category = "Data"
    };

    // Administrative Permissions
    public static readonly Permission AdminTenantRead = new()
    {
        Id = "admin:tenant:read",
        Name = "Read Tenant Configuration",
        Description = "Can view tenant configuration and settings",
        Category = "Admin"
    };

    public static readonly Permission AdminTenantWrite = new()
    {
        Id = "admin:tenant:write",
        Name = "Modify Tenant Configuration",
        Description = "Can modify tenant configuration and settings",
        Category = "Admin",
        RequiresElevation = true
    };

    public static readonly Permission AdminApiKeys = new()
    {
        Id = "admin:apikeys",
        Name = "API Key Management",
        Description = "Can create, modify, and revoke API keys",
        Category = "Admin",
        RequiresElevation = true
    };

    public static readonly Permission AdminUsage = new()
    {
        Id = "admin:usage",
        Name = "Usage Analytics",
        Description = "Can view usage analytics and billing information",
        Category = "Admin"
    };

    public static readonly Permission AdminIntegrations = new()
    {
        Id = "admin:integrations",
        Name = "Integration Management",
        Description = "Can configure and manage integrations",
        Category = "Admin",
        RequiresElevation = true
    };

    /// <summary>
    /// Gets all available permissions
    /// </summary>
    public static IReadOnlyList<Permission> All => new[]
    {
        FleetQuery, FleetQueryAdvanced,
        AgentFuel, AgentMaintenance, AgentSafety, AgentLocation, AgentCompliance, AgentFinancial,
        IntegrationGeotab, IntegrationFleetio, IntegrationSamsara,
        DataRealTime, DataHistorical, DataHistoricalExtended, DataExport,
        AdminTenantRead, AdminTenantWrite, AdminApiKeys, AdminUsage, AdminIntegrations
    };

    /// <summary>
    /// Gets permission by ID
    /// </summary>
    public static Permission? GetById(string permissionId)
    {
        return All.FirstOrDefault(p => p.Id == permissionId);
    }

    /// <summary>
    /// Gets permissions by category
    /// </summary>
    public static IReadOnlyList<Permission> GetByCategory(string category)
    {
        return All.Where(p => p.Category == category).ToList();
    }
}

/// <summary>
/// Predefined roles with standard permission sets
/// </summary>
public static class Roles
{
    /// <summary>
    /// Basic fleet user - can query basic fleet information
    /// </summary>
    public static readonly List<string> FleetUser = new()
    {
        Permissions.FleetQuery.Id,
        Permissions.AgentFuel.Id,
        Permissions.AgentMaintenance.Id,
        Permissions.AgentSafety.Id,
        Permissions.DataRealTime.Id,
        Permissions.DataHistorical.Id
    };

    /// <summary>
    /// Advanced fleet analyst - can use advanced features and all agents
    /// </summary>
    public static readonly List<string> FleetAnalyst = new()
    {
        Permissions.FleetQuery.Id,
        Permissions.FleetQueryAdvanced.Id,
        Permissions.AgentFuel.Id,
        Permissions.AgentMaintenance.Id,
        Permissions.AgentSafety.Id,
        Permissions.AgentLocation.Id,
        Permissions.AgentCompliance.Id,
        Permissions.AgentFinancial.Id,
        Permissions.DataRealTime.Id,
        Permissions.DataHistorical.Id,
        Permissions.DataHistoricalExtended.Id,
        Permissions.DataExport.Id
    };

    /// <summary>
    /// Integration admin - can manage integrations and connections
    /// </summary>
    public static readonly List<string> IntegrationAdmin = new()
    {
        Permissions.FleetQuery.Id,
        Permissions.IntegrationGeotab.Id,
        Permissions.IntegrationFleetio.Id,
        Permissions.IntegrationSamsara.Id,
        Permissions.AdminIntegrations.Id,
        Permissions.AdminUsage.Id,
        Permissions.DataRealTime.Id,
        Permissions.DataHistorical.Id
    };

    /// <summary>
    /// Tenant administrator - full access to tenant management
    /// </summary>
    public static readonly List<string> TenantAdmin = new()
    {
        Permissions.FleetQuery.Id,
        Permissions.FleetQueryAdvanced.Id,
        Permissions.AgentFuel.Id,
        Permissions.AgentMaintenance.Id,
        Permissions.AgentSafety.Id,
        Permissions.AgentLocation.Id,
        Permissions.AgentCompliance.Id,
        Permissions.AgentFinancial.Id,
        Permissions.IntegrationGeotab.Id,
        Permissions.IntegrationFleetio.Id,
        Permissions.IntegrationSamsara.Id,
        Permissions.DataRealTime.Id,
        Permissions.DataHistorical.Id,
        Permissions.DataHistoricalExtended.Id,
        Permissions.DataExport.Id,
        Permissions.AdminTenantRead.Id,
        Permissions.AdminTenantWrite.Id,
        Permissions.AdminApiKeys.Id,
        Permissions.AdminUsage.Id,
        Permissions.AdminIntegrations.Id
    };

    /// <summary>
    /// Read-only viewer - can only view basic information
    /// </summary>
    public static readonly List<string> Viewer = new()
    {
        Permissions.FleetQuery.Id,
        Permissions.DataRealTime.Id,
        Permissions.AdminUsage.Id
    };

    /// <summary>
    /// Gets permissions for a predefined role
    /// </summary>
    public static List<string> GetRolePermissions(string roleName)
    {
        return roleName.ToLowerInvariant() switch
        {
            "fleetuser" => FleetUser,
            "fleetanalyst" => FleetAnalyst,
            "integrationadmin" => IntegrationAdmin,
            "tenantadmin" => TenantAdmin,
            "viewer" => Viewer,
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Gets all available role names
    /// </summary>
    public static IReadOnlyList<string> AllRoleNames => new[]
    {
        "FleetUser", "FleetAnalyst", "IntegrationAdmin", "TenantAdmin", "Viewer"
    };
}
