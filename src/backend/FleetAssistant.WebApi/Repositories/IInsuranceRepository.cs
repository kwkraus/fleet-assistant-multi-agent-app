using FleetAssistant.Shared.Models;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Repository interface for insurance policy operations
/// </summary>
public interface IInsuranceRepository : IRepository<InsurancePolicy>
{
    /// <summary>
    /// Gets insurance policies for a specific vehicle
    /// </summary>
    Task<IEnumerable<InsurancePolicy>> GetByVehicleIdAsync(int vehicleId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets insurance policies by provider
    /// </summary>
    Task<IEnumerable<InsurancePolicy>> GetByProviderAsync(string provider, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets active insurance policies
    /// </summary>
    Task<IEnumerable<InsurancePolicy>> GetActiveAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets policies expiring within a specified number of days
    /// </summary>
    Task<IEnumerable<InsurancePolicy>> GetExpiringAsync(int daysFromNow = 30, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets policies by coverage type
    /// </summary>
    Task<IEnumerable<InsurancePolicy>> GetByCoverageTypeAsync(string coverageType, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets insurance statistics
    /// </summary>
    Task<object> GetInsuranceStatsAsync();

    /// <summary>
    /// Gets total premium cost for a vehicle within a date range
    /// </summary>
    Task<decimal> GetTotalPremiumCostAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null);
}
