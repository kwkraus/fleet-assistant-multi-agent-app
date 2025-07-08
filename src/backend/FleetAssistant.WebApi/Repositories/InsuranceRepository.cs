using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Repository implementation for InsurancePolicy operations
/// </summary>
public class InsuranceRepository(FleetAssistantDbContext context, ILogger<InsuranceRepository> logger) : Repository<InsurancePolicy>(context, logger), IInsuranceRepository
{
    public async Task<IEnumerable<InsurancePolicy>> GetByVehicleIdAsync(int vehicleId, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _dbSet
                .Where(i => i.VehicleInsurances.Any(vi => vi.VehicleId == vehicleId && vi.IsActive))
                .OrderByDescending(i => i.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance policies for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }

    public async Task<IEnumerable<InsurancePolicy>> GetByProviderAsync(string provider, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _dbSet
                .Where(i => i.ProviderName == provider)
                .OrderByDescending(i => i.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance policies for provider {Provider}", provider);
            throw;
        }
    }

    public async Task<InsurancePolicy?> GetByPolicyNumberAsync(string policyNumber)
    {
        try
        {
            return await _dbSet
                .Include(i => i.VehicleInsurances)
                    .ThenInclude(vi => vi.Vehicle)
                .FirstOrDefaultAsync(i => i.PolicyNumber == policyNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance policy with policy number {PolicyNumber}", policyNumber);
            throw;
        }
    }

    public async Task<IEnumerable<InsurancePolicy>> GetActiveAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var today = DateTime.UtcNow.Date;

            return await _dbSet
                .Where(i => i.StartDate <= today && i.EndDate >= today)
                .OrderBy(i => i.EndDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active insurance policies");
            throw;
        }
    }

    public async Task<IEnumerable<InsurancePolicy>> GetExpiringAsync(int daysFromNow = 30, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var today = DateTime.UtcNow.Date;
            var expiryDate = today.AddDays(daysFromNow);

            return await _dbSet
                .Where(i => i.EndDate >= today && i.EndDate <= expiryDate)
                .OrderBy(i => i.EndDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring insurance policies");
            throw;
        }
    }

    public async Task<IEnumerable<InsurancePolicy>> GetByCoverageTypeAsync(string coverageType, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Try to parse the string to the enum
            if (Enum.TryParse<InsuranceCoverageType>(coverageType, out var parsedCoverageType))
            {
                return await _dbSet
                    .Where(i => i.CoverageType == parsedCoverageType)
                    .OrderByDescending(i => i.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            else
            {
                // Return empty list if coverage type doesn't match any enum value
                return [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance policies for coverage type {CoverageType}", coverageType);
            throw;
        }
    }

    public async Task<object> GetInsuranceStatsAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            var totalPolicies = await _dbSet.CountAsync();
            var activePolicies = await _dbSet.CountAsync(i => i.StartDate <= today && i.EndDate >= today);
            var expiringIn30Days = await _dbSet.CountAsync(i => i.EndDate >= today && i.EndDate <= today.AddDays(30));
            var expiredPolicies = await _dbSet.CountAsync(i => i.EndDate < today);

            var totalPremiumCost = await _dbSet.SumAsync(i => i.PremiumAmount);
            var averagePremium = totalPolicies > 0 ? totalPremiumCost / totalPolicies : 0;

            var providerStats = await _dbSet
                .GroupBy(i => i.ProviderName)
                .Select(g => new
                {
                    Provider = g.Key,
                    PolicyCount = g.Count(),
                    TotalPremium = g.Sum(i => i.PremiumAmount),
                    ActivePolicies = g.Count(i => i.StartDate <= today && i.EndDate >= today)
                })
                .ToListAsync();

            var coverageTypeStats = await _dbSet
                .GroupBy(i => i.CoverageType)
                .Select(g => new
                {
                    CoverageType = g.Key,
                    PolicyCount = g.Count(),
                    TotalPremium = g.Sum(i => i.PremiumAmount)
                })
                .ToListAsync();

            return new
            {
                TotalPolicies = totalPolicies,
                ActivePolicies = activePolicies,
                ExpiringIn30Days = expiringIn30Days,
                ExpiredPolicies = expiredPolicies,
                TotalPremiumCost = Math.Round(totalPremiumCost, 2),
                AveragePremium = Math.Round(averagePremium, 2),
                ProviderBreakdown = providerStats,
                CoverageTypeBreakdown = coverageTypeStats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating insurance statistics");
            throw;
        }
    }

    public async Task<decimal> GetTotalPremiumCostAsync(int vehicleId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _dbSet.Where(i => i.VehicleInsurances.Any(vi => vi.VehicleId == vehicleId && vi.IsActive));

            if (startDate.HasValue)
                query = query.Where(i => i.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(i => i.EndDate <= endDate.Value);

            return await query.SumAsync(i => i.PremiumAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total premium cost for vehicle {VehicleId}", vehicleId);
            throw;
        }
    }
}
