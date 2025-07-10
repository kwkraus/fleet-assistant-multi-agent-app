using FleetAssistant.Shared.Models;

namespace FleetAssistant.WebApi.Repositories.Interfaces;

/// <summary>
/// Repository interface for FuelLog entities with specialized operations
/// </summary>
public interface IFuelLogRepository : IRepository<FuelLog>
{
    /// <summary>
    /// Get fuel logs for a specific vehicle
    /// </summary>
    Task<IEnumerable<FuelLog>> GetByVehicleIdAsync(int vehicleId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get fuel logs within a date range
    /// </summary>
    Task<IEnumerable<FuelLog>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int? vehicleId = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get fuel statistics for a vehicle
    /// </summary>
    Task<object> GetVehicleStatisticsAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Calculate and update MPG for a fuel log entry
    /// </summary>
    Task<FuelLog> CalculateAndUpdateMpgAsync(FuelLog fuelLog);

    /// <summary>
    /// Get the previous fuel log for MPG calculation
    /// </summary>
    Task<FuelLog?> GetPreviousFuelLogAsync(int vehicleId, DateTime fuelDate);

    /// <summary>
    /// Get fuel efficiency trends for a vehicle
    /// </summary>
    Task<IEnumerable<object>> GetEfficiencyTrendsAsync(int vehicleId, int months = 6);
}
