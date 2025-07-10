using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Data;
using FleetAssistant.WebApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Repository implementation for VehicleFinancial operations
/// </summary>
public class FinancialRepository(FleetAssistantDbContext context, ILogger<FinancialRepository> logger) : Repository<VehicleFinancial>(context, logger), IFinancialRepository
{
    public async Task<IEnumerable<VehicleFinancial>> GetByVehicleIdAsync(int vehicleId, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _dbSet
                .Where(f => f.VehicleId == vehicleId)
                .OrderByDescending(f => f.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial records for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }

    public async Task<IEnumerable<VehicleFinancial>> GetByTransactionTypeAsync(string transactionType, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Try to parse the string to the enum
            if (Enum.TryParse<FinancialType>(transactionType, true, out var parsedType))
            {
                return await _dbSet
                    .Where(f => f.FinancialType == parsedType)
                    .OrderByDescending(f => f.StartDate)
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
            _logger.LogError(ex, "Error retrieving financial records for transaction type {TransactionType}", transactionType);
            throw;
        }
    }

    public async Task<IEnumerable<VehicleFinancial>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _dbSet
                .Where(f => f.StartDate >= startDate && f.StartDate <= endDate)
                .OrderByDescending(f => f.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial records for date range {StartDate} - {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<VehicleFinancial>> GetByCategoryAsync(string category, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Since the model doesn't have a Category property, we'll use FinancialType instead
            if (Enum.TryParse<FinancialType>(category, out var parsedType))
            {
                return await _dbSet
                    .Where(f => f.FinancialType == parsedType)
                    .OrderByDescending(f => f.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            else
            {
                // Return empty list if category doesn't match any enum value
                return [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial records for category {Category}", category);
            throw;
        }
    }

    public async Task<object> GetFinancialStatsAsync(int vehicleId)
    {
        try
        {
            var financialRecords = await _dbSet
                .Where(f => f.VehicleId == vehicleId)
                .ToListAsync();

            if (financialRecords.Count == 0)
            {
                return new
                {
                    VehicleId = vehicleId,
                    TotalRecords = 0,
                    TotalObligations = 0m,
                    MonthlyPayments = 0m,
                    RemainingBalance = 0m,
                    PurchasePrice = 0m,
                    CurrentValue = 0m,
                    DepreciationAmount = 0m,
                    FinancialTypeBreakdown = new List<object>()
                };
            }

            var totalObligations = financialRecords.Sum(f => f.Amount);
            var monthlyPayments = financialRecords
                .Where(f => f.PaymentFrequency == PaymentFrequency.Monthly)
                .Sum(f => f.Amount);
            var totalRemainingBalance = financialRecords.Sum(f => f.RemainingBalance ?? 0);
            var purchasePrice = financialRecords.FirstOrDefault(f => f.FinancialType == FinancialType.Purchase)?.PurchasePrice ?? 0;
            var currentValue = financialRecords.Max(f => f.CurrentValue ?? 0);
            var depreciationAmount = purchasePrice - currentValue;

            var financialTypeBreakdown = financialRecords
                .GroupBy(f => f.FinancialType)
                .Select(g => new
                {
                    FinancialType = g.Key.ToString(),
                    TotalAmount = g.Sum(f => f.Amount),
                    RecordCount = g.Count(),
                    RemainingBalance = g.Sum(f => f.RemainingBalance ?? 0)
                })
                .ToList();

            return new
            {
                VehicleId = vehicleId,
                TotalRecords = financialRecords.Count,
                TotalObligations = Math.Round(totalObligations, 2),
                MonthlyPayments = Math.Round(monthlyPayments, 2),
                RemainingBalance = Math.Round(totalRemainingBalance, 2),
                PurchasePrice = Math.Round(purchasePrice, 2),
                CurrentValue = Math.Round(currentValue, 2),
                DepreciationAmount = Math.Round(depreciationAmount, 2),
                FinancialTypeBreakdown = financialTypeBreakdown
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating financial statistics for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }

    public async Task<decimal> GetTotalCostByCategoryAsync(int vehicleId, string category, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            // Try to parse the category as FinancialType
            if (Enum.TryParse<FinancialType>(category, out var parsedType))
            {
                var query = _dbSet.Where(f => f.VehicleId == vehicleId && f.FinancialType == parsedType);

                if (startDate.HasValue)
                    query = query.Where(f => f.StartDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(f => f.EndDate <= endDate.Value || f.EndDate == null);

                return await query.SumAsync(f => f.Amount);
            }
            else
            {
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total cost for vehicle {VehicleId} category {Category}", vehicleId, category);
            throw;
        }
    }

    public async Task<object> GetProfitLossSummaryAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _dbSet.Where(f => f.VehicleId == vehicleId);

            if (startDate.HasValue)
                query = query.Where(f => f.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.EndDate <= endDate.Value || f.EndDate == null);

            var records = await query.ToListAsync();

            // For VehicleFinancial, we'll calculate costs vs current value
            var totalCosts = records.Sum(f => f.Amount);
            var currentValue = records.Max(f => f.CurrentValue ?? 0);
            var purchasePrice = records.FirstOrDefault(f => f.FinancialType == FinancialType.Purchase)?.PurchasePrice ?? 0;
            var totalDepreciation = purchasePrice - currentValue;

            var monthlyData = records
                .GroupBy(f => new { f.StartDate.Year, f.StartDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    TotalCosts = g.Sum(f => f.Amount),
                    PaymentCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            return new
            {
                VehicleId = vehicleId,
                PeriodStart = startDate?.ToString("yyyy-MM-dd") ?? "All Time",
                PeriodEnd = endDate?.ToString("yyyy-MM-dd") ?? "All Time",
                TotalCosts = Math.Round(totalCosts, 2),
                CurrentValue = Math.Round(currentValue, 2),
                PurchasePrice = Math.Round(purchasePrice, 2),
                TotalDepreciation = Math.Round(totalDepreciation, 2),
                MonthlyBreakdown = monthlyData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating profit/loss summary for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }

    public async Task<object> GetFleetFinancialSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _dbSet.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(f => f.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.EndDate <= endDate.Value || f.EndDate == null);

            var records = await query.ToListAsync();

            var totalCosts = records.Sum(f => f.Amount);
            var totalCurrentValue = records.GroupBy(f => f.VehicleId).Sum(g => g.Max(f => f.CurrentValue ?? 0));
            var totalPurchasePrice = records.GroupBy(f => f.VehicleId).Sum(g => g.Max(f => f.PurchasePrice ?? 0));
            var totalDepreciation = totalPurchasePrice - totalCurrentValue;

            var vehicleBreakdown = records
                .GroupBy(f => f.VehicleId)
                .Select(g => new
                {
                    VehicleId = g.Key,
                    TotalCosts = g.Sum(f => f.Amount),
                    CurrentValue = g.Max(f => f.CurrentValue ?? 0),
                    PurchasePrice = g.Max(f => f.PurchasePrice ?? 0),
                    RecordCount = g.Count()
                })
                .OrderByDescending(x => x.TotalCosts)
                .ToList();

            var financialTypeBreakdown = records
                .GroupBy(f => f.FinancialType)
                .Select(g => new
                {
                    FinancialType = g.Key.ToString(),
                    TotalAmount = g.Sum(f => f.Amount),
                    RecordCount = g.Count()
                })
                .ToList();

            return new
            {
                PeriodStart = startDate?.ToString("yyyy-MM-dd") ?? "All Time",
                PeriodEnd = endDate?.ToString("yyyy-MM-dd") ?? "All Time",
                FleetTotalCosts = Math.Round(totalCosts, 2),
                FleetCurrentValue = Math.Round(totalCurrentValue, 2),
                FleetPurchasePrice = Math.Round(totalPurchasePrice, 2),
                FleetDepreciation = Math.Round(totalDepreciation, 2),
                VehicleCount = vehicleBreakdown.Count,
                TotalRecords = records.Count,
                VehicleBreakdown = vehicleBreakdown,
                FinancialTypeBreakdown = financialTypeBreakdown
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating fleet financial summary");
            throw;
        }
    }
}
