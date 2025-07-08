using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Repository implementation for MaintenanceRecord operations
/// </summary>
public class MaintenanceRepository(FleetAssistantDbContext context, ILogger<MaintenanceRepository> logger) : Repository<MaintenanceRecord>(context, logger), IMaintenanceRepository
{
    public async Task<IEnumerable<MaintenanceRecord>> GetByVehicleIdAsync(int vehicleId, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _dbSet
                .Where(m => m.VehicleId == vehicleId)
                .OrderByDescending(m => m.MaintenanceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance records for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetByTypeAsync(string maintenanceType, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Parse the string to enum if possible
            if (Enum.TryParse<MaintenanceType>(maintenanceType, out var parsedType))
            {
                return await _dbSet
                    .Where(m => m.MaintenanceType == parsedType)
                    .OrderByDescending(m => m.MaintenanceDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            else
            {
                // Return empty list if type doesn't match any enum value
                return [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance records for type {MaintenanceType}", maintenanceType);
            throw;
        }
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _dbSet
                .Where(m => m.MaintenanceDate >= startDate && m.MaintenanceDate <= endDate)
                .OrderByDescending(m => m.MaintenanceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance records for date range {StartDate} - {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetUpcomingMaintenanceAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var today = DateTime.UtcNow.Date;

            return await _dbSet
                .Where(m => m.NextMaintenanceDate.HasValue && m.NextMaintenanceDate.Value >= today)
                .OrderBy(m => m.NextMaintenanceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming maintenance records");
            throw;
        }
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetOverdueMaintenanceAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var today = DateTime.UtcNow.Date;

            return await _dbSet
                .Where(m => m.NextMaintenanceDate.HasValue && m.NextMaintenanceDate.Value < today)
                .OrderBy(m => m.NextMaintenanceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue maintenance records");
            throw;
        }
    }

    public async Task<object> GetMaintenanceStatsAsync(int vehicleId)
    {
        try
        {
            var maintenanceRecords = await _dbSet
                .Where(m => m.VehicleId == vehicleId)
                .ToListAsync();

            if (maintenanceRecords.Count == 0)
            {
                return new
                {
                    VehicleId = vehicleId,
                    TotalMaintenanceRecords = 0,
                    TotalCost = 0m,
                    AverageCost = 0m,
                    CompletedMaintenance = 0,
                    PendingMaintenance = 0,
                    OverdueMaintenance = 0,
                    MaintenanceByType = new List<object>()
                };
            }

            var totalCost = maintenanceRecords.Sum(m => m.Cost);
            var averageCost = maintenanceRecords.Average(m => m.Cost);
            var completedMaintenance = maintenanceRecords.Count(m => m.MaintenanceDate <= DateTime.UtcNow);
            var scheduledMaintenance = maintenanceRecords.Count(m => m.NextMaintenanceDate.HasValue);

            var today = DateTime.UtcNow.Date;
            var overdueMaintenance = maintenanceRecords.Count(m =>
                m.NextMaintenanceDate.HasValue &&
                m.NextMaintenanceDate.Value < today);

            var maintenanceByType = maintenanceRecords
                .GroupBy(m => m.MaintenanceType)
                .Select(g => new
                {
                    MaintenanceType = g.Key,
                    Count = g.Count(),
                    TotalCost = g.Sum(m => m.Cost),
                    AverageCost = g.Average(m => m.Cost)
                })
                .ToList();

            return new
            {
                VehicleId = vehicleId,
                TotalMaintenanceRecords = maintenanceRecords.Count,
                TotalCost = Math.Round(totalCost, 2),
                AverageCost = Math.Round(averageCost, 2),
                CompletedMaintenance = completedMaintenance,
                ScheduledMaintenance = scheduledMaintenance,
                OverdueMaintenance = overdueMaintenance,
                MaintenanceByType = maintenanceByType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating maintenance statistics for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }

    public async Task<decimal> GetTotalCostAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _dbSet.Where(m => m.VehicleId == vehicleId);

            if (startDate.HasValue)
                query = query.Where(m => m.MaintenanceDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(m => m.MaintenanceDate <= endDate.Value);

            return await query.SumAsync(m => m.Cost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total maintenance cost for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }
}
