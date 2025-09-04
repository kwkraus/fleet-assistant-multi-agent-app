using System.Threading.Tasks;

namespace FleetAssistant.WebApi.Data.Seeding;

/// <summary>
/// Contract for database seeding used in development / in-memory scenarios.
/// Idempotent: calling multiple times should not duplicate data.
/// </summary>
public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
