namespace FleetAssistant.WebApi.Repositories;

/// <summary>
/// Generic repository interface for common CRUD operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get all entities with optional filtering and pagination
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(
        Func<IQueryable<T>, IQueryable<T>>? filter = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get entity by ID
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Add new entity
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Update existing entity
    /// </summary>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Delete entity by ID
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Check if entity exists
    /// </summary>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Get count of entities with optional filtering
    /// </summary>
    Task<int> CountAsync(Func<IQueryable<T>, IQueryable<T>>? filter = null);
}
