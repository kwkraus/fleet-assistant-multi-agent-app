using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Repository implementation for FuelLog operations
/// </summary>
public class FuelLogRepository : Repository<FuelLog>, IFuelLogRepository
{
    public FuelLogRepository(FleetAssistantDbContext context, ILogger<FuelLogRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<FuelLog>> GetByVehicleIdAsync(int vehicleId, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _dbSet
                .Where(f => f.VehicleId == vehicleId)
                .OrderByDescending(f => f.FuelDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fuel logs for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }

    public async Task<IEnumerable<FuelLog>> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        int? vehicleId = null,
        int page = 1, 
        int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _dbSet.Where(f => f.FuelDate >= startDate && f.FuelDate <= endDate);

            if (vehicleId.HasValue)
            {
                query = query.Where(f => f.VehicleId == vehicleId.Value);
            }

            return await query
                .OrderByDescending(f => f.FuelDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fuel logs for date range {StartDate} - {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<object> GetVehicleStatisticsAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _dbSet.Where(f => f.VehicleId == vehicleId);

            if (startDate.HasValue)
                query = query.Where(f => f.FuelDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.FuelDate <= endDate.Value);

            var fuelLogs = await query.OrderBy(f => f.FuelDate).ToListAsync();

            if (!fuelLogs.Any())
            {
                return new
                {
                    VehicleId = vehicleId,
                    TotalFuelLogs = 0,
                    TotalCost = 0m,
                    TotalGallons = 0m,
                    AveragePricePerGallon = 0m,
                    AverageFuelEfficiency = 0m,
                    BestFuelEfficiency = 0m,
                    WorstFuelEfficiency = 0m,
                    TrendAnalysis = "No data available"
                };
            }

            var totalCost = fuelLogs.Sum(f => f.TotalCost);
            var totalGallons = fuelLogs.Sum(f => f.Gallons);
            var averagePricePerGallon = totalGallons > 0 ? totalCost / totalGallons : 0;

            var logsWithEfficiency = fuelLogs.Where(f => f.MilesPerGallon.HasValue).ToList();
            var averageEfficiency = logsWithEfficiency.Any() ? logsWithEfficiency.Average(f => f.MilesPerGallon!.Value) : 0;
            var bestEfficiency = logsWithEfficiency.Any() ? logsWithEfficiency.Max(f => f.MilesPerGallon!.Value) : 0;
            var worstEfficiency = logsWithEfficiency.Any() ? logsWithEfficiency.Min(f => f.MilesPerGallon!.Value) : 0;

            // Simple trend analysis - compare last 3 vs previous 3 logs
            var recentLogs = logsWithEfficiency.TakeLast(3).ToList();
            var previousLogs = logsWithEfficiency.Skip(Math.Max(0, logsWithEfficiency.Count - 6)).Take(3).ToList();

            string trendAnalysis = "Stable";
            if (recentLogs.Count >= 3 && previousLogs.Count >= 3)
            {
                var recentAvg = recentLogs.Average(f => f.MilesPerGallon!.Value);
                var previousAvg = previousLogs.Average(f => f.MilesPerGallon!.Value);

                if (recentAvg > previousAvg * 1.05m) trendAnalysis = "Improving";
                else if (recentAvg < previousAvg * 0.95m) trendAnalysis = "Declining";
            }

            return new
            {
                VehicleId = vehicleId,
                TotalFuelLogs = fuelLogs.Count,
                TotalCost = Math.Round(totalCost, 2),
                TotalGallons = Math.Round(totalGallons, 2),
                AveragePricePerGallon = Math.Round(averagePricePerGallon, 2),
                AverageFuelEfficiency = Math.Round(averageEfficiency, 2),
                BestFuelEfficiency = Math.Round(bestEfficiency, 2),
                WorstFuelEfficiency = Math.Round(worstEfficiency, 2),
                TrendAnalysis = trendAnalysis
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicle statistics for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }

    public async Task<FuelLog> CalculateAndUpdateMpgAsync(FuelLog fuelLog)
    {
        try
        {
            // Get the previous fuel log to calculate MPG
            var previousLog = await GetPreviousFuelLogAsync(fuelLog.VehicleId, fuelLog.FuelDate);

            if (previousLog != null && fuelLog.OdometerReading > previousLog.OdometerReading)
            {
                var milesDriven = fuelLog.OdometerReading - previousLog.OdometerReading;
                fuelLog.MilesDriven = milesDriven;
                if (fuelLog.Gallons > 0)
                {
                    fuelLog.MilesPerGallon = milesDriven / fuelLog.Gallons;
                }
            }

            // Update the fuel log in the database
            _dbSet.Update(fuelLog);
            await _context.SaveChangesAsync();

            return fuelLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating and updating MPG for fuel log {FuelLogId}", fuelLog.Id);
            throw;
        }
    }

    public async Task<FuelLog?> GetPreviousFuelLogAsync(int vehicleId, DateTime fuelDate)
    {
        try
        {
            return await _dbSet
                .Where(f => f.VehicleId == vehicleId && f.FuelDate < fuelDate)
                .OrderByDescending(f => f.FuelDate)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting previous fuel log for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }

    public async Task<IEnumerable<object>> GetEfficiencyTrendsAsync(int vehicleId, int months = 6)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            var fuelLogs = await _dbSet
                .Where(f => f.VehicleId == vehicleId && f.FuelDate >= startDate && f.MilesPerGallon.HasValue)
                .OrderBy(f => f.FuelDate)
                .ToListAsync();

            var monthlyData = fuelLogs
                .GroupBy(f => new { f.FuelDate.Year, f.FuelDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    AverageEfficiency = g.Average(f => f.MilesPerGallon!.Value),
                    BestEfficiency = g.Max(f => f.MilesPerGallon!.Value),
                    WorstEfficiency = g.Min(f => f.MilesPerGallon!.Value),
                    FuelLogCount = g.Count(),
                    TotalCost = g.Sum(f => f.TotalCost),
                    TotalGallons = g.Sum(f => f.Gallons)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            return monthlyData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting efficiency trends for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }
}
