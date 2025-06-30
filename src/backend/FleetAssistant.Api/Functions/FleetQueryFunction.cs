using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FleetAssistant.Api.Functions;

/// <summary>
/// Main HTTP endpoint for fleet queries
/// </summary>
public class FleetQueryFunction(
    ILogger<FleetQueryFunction> logger)
{
    private readonly ILogger<FleetQueryFunction> _logger = logger;

    [Function("FleetQuery")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fleet/query")] HttpRequest req)
    {

        throw new NotImplementedException("FleetQueryFunction is not implemented yet. Please implement the logic for handling fleet queries.");
    }
}
