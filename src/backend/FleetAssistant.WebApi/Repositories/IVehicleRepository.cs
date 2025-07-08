using FleetAssistant.Shared.Models;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Repository interface for Vehicle entities with specialized operations
/// </summary>
public interface IVehicleRepository : IRepository<Vehicle>
{
    /// <summary>
    /// Get vehicle by VIN
    /// </summary>
    Task<Vehicle?> GetByVinAsync(string vin);

    /// <summary>
    /// Check if VIN exists (excluding specific vehicle ID)
    /// </summary>
    Task<bool> VinExistsAsync(string vin, int? excludeVehicleId = null);

    /// <summary>
    /// Get vehicles by status
    /// </summary>
    Task<IEnumerable<Vehicle>> GetByStatusAsync(VehicleStatus status, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get vehicle statistics
    /// </summary>
    Task<object> GetStatisticsAsync();

    /// <summary>
    /// Get vehicles with their latest fuel log for efficiency tracking
    /// </summary>
    Task<IEnumerable<Vehicle>> GetVehiclesWithLatestFuelLogAsync(int page = 1, int pageSize = 20);
}
