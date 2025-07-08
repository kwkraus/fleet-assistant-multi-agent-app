using FleetAssistant.Shared.Models;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Repository interface for vehicle financial operations
/// </summary>
public interface IFinancialRepository : IRepository<VehicleFinancial>
{
    /// <summary>
    /// Gets financial records for a specific vehicle
    /// </summary>
    Task<IEnumerable<VehicleFinancial>> GetByVehicleIdAsync(int vehicleId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets financial records by transaction type
    /// </summary>
    Task<IEnumerable<VehicleFinancial>> GetByTransactionTypeAsync(string transactionType, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets financial records within a date range
    /// </summary>
    Task<IEnumerable<VehicleFinancial>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets financial records by category
    /// </summary>
    Task<IEnumerable<VehicleFinancial>> GetByCategoryAsync(string category, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets financial statistics for a vehicle
    /// </summary>
    Task<object> GetFinancialStatsAsync(int vehicleId);

    /// <summary>
    /// Gets total cost for a vehicle within a date range by category
    /// </summary>
    Task<decimal> GetTotalCostByCategoryAsync(int vehicleId, string category, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets profit/loss summary for a vehicle
    /// </summary>
    Task<object> GetProfitLossSummaryAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets fleet-wide financial summary
    /// </summary>
    Task<object> GetFleetFinancialSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
}
