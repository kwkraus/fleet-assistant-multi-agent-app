using FleetAssistant.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Generic repository implementation for common CRUD operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T>(FleetAssistantDbContext context, ILogger<Repository<T>> logger) : IRepository<T> where T : class
{
    protected readonly FleetAssistantDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();
    protected readonly ILogger<Repository<T>> _logger = logger;

    public virtual async Task<IEnumerable<T>> GetAllAsync(
        Func<IQueryable<T>, IQueryable<T>>? filter = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = filter(query);
            }

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        try
        {
            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        try
        {
            var entry = await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        try
        {
            // Use a more efficient approach than GetByIdAsync for existence check
            var keyProperty = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties?.FirstOrDefault();
            if (keyProperty?.Name == "Id")
            {
                return await _dbSet.AnyAsync(e => EF.Property<int>(e, "Id") == id);
            }

            // Fallback to FindAsync if we can't use the optimized approach
            return await _dbSet.FindAsync(id) != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<int> CountAsync(Func<IQueryable<T>, IQueryable<T>>? filter = null)
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = filter(query);
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }
}
