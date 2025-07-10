using FleetAssistant.Shared.Models;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Repository interface for maintenance record operations
/// </summary>
public interface IMaintenanceRepository : IRepository<MaintenanceRecord>
{
    /// <summary>
    /// Gets maintenance records for a specific vehicle
    /// </summary>
    Task<IEnumerable<MaintenanceRecord>> GetByVehicleIdAsync(int vehicleId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets maintenance records by type
    /// </summary>
    Task<IEnumerable<MaintenanceRecord>> GetByTypeAsync(string maintenanceType, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets maintenance records within a date range
    /// </summary>
    Task<IEnumerable<MaintenanceRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets upcoming maintenance (scheduled but not completed)
    /// </summary>
    Task<IEnumerable<MaintenanceRecord>> GetUpcomingMaintenanceAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets overdue maintenance records
    /// </summary>
    Task<IEnumerable<MaintenanceRecord>> GetOverdueMaintenanceAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets maintenance statistics for a vehicle
    /// </summary>
    Task<object> GetMaintenanceStatsAsync(int vehicleId);

    /// <summary>
    /// Gets total maintenance cost for a vehicle within a date range
    /// </summary>
    Task<decimal> GetTotalCostAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null);
}
