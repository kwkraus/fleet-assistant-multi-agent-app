using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Repository implementation for Vehicle operations
/// </summary>
public class VehicleRepository(FleetAssistantDbContext context, ILogger<VehicleRepository> logger) : Repository<Vehicle>(context, logger), IVehicleRepository
{
    public async Task<Vehicle?> GetByVinAsync(string vin)
    {
        try
        {
            return await _dbSet.FirstOrDefaultAsync(v => v.Vin == vin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicle by VIN {VIN}", vin);
            throw;
        }
    }

    public async Task<bool> VinExistsAsync(string vin, int? excludeVehicleId = null)
    {
        try
        {
            var query = _dbSet.AsQueryable();

            if (excludeVehicleId.HasValue)
            {
                query = query.Where(v => v.Id != excludeVehicleId.Value);
            }

            return await query.AnyAsync(v => v.Vin == vin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking VIN existence for {VIN}", vin);
            throw;
        }
    }

    public async Task<IEnumerable<Vehicle>> GetByStatusAsync(VehicleStatus status, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _dbSet
                .Where(v => v.Status == status)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicles by status {Status}", status);
            throw;
        }
    }

    public async Task<object> GetStatisticsAsync()
    {
        try
        {
            var totalVehicles = await _dbSet.CountAsync();
            var activeVehicles = await _dbSet.CountAsync(v => v.Status == VehicleStatus.Active);
            var outOfServiceVehicles = await _dbSet.CountAsync(v => v.Status == VehicleStatus.OutOfService);
            var maintenanceVehicles = await _dbSet.CountAsync(v => v.Status == VehicleStatus.InMaintenance);

            var vehiclesByMake = await _dbSet
                .GroupBy(v => v.Make)
                .Select(g => new { Make = g.Key, Count = g.Count() })
                .ToListAsync();

            var averageYear = await _dbSet.AverageAsync(v => v.Year);
            var averageOdometer = await _dbSet.AverageAsync(v => v.OdometerReading);

            return new
            {
                TotalVehicles = totalVehicles,
                ActiveVehicles = activeVehicles,
                OutOfServiceVehicles = outOfServiceVehicles,
                MaintenanceVehicles = maintenanceVehicles,
                VehiclesByMake = vehiclesByMake,
                AverageYear = Math.Round(averageYear, 1),
                AverageOdometer = Math.Round(averageOdometer, 0)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating vehicle statistics");
            throw;
        }
    }

    public async Task<IEnumerable<Vehicle>> GetVehiclesWithLatestFuelLogAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _dbSet
                .Include(v => v.FuelLogs.OrderByDescending(f => f.FuelDate).Take(1))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicles with latest fuel logs");
            throw;
        }
    }
}
