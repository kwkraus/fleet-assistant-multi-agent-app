using FleetAssistant.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FleetAssistant.WebApi.Controllers;

/// <summary>
/// API controller for managing insurance policies
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InsuranceController : ControllerBase
{
    private readonly ILogger<InsuranceController> _logger;

    public InsuranceController(ILogger<InsuranceController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all insurance policies with optional filtering
    /// </summary>
    /// <param name="providerName">Filter by provider name</param>
    /// <param name="coverageType">Filter by coverage type</param>
    /// <param name="isActive">Filter by active status (policies that are currently valid)</param>
    /// <param name="expiringInDays">Filter policies expiring within specified days</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>List of insurance policies matching the criteria</returns>
    /// <response code="200">Returns the list of insurance policies</response>
    /// <response code="400">If the request parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InsurancePolicyDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<ActionResult<IEnumerable<InsurancePolicyDto>>> GetInsurancePolicies(
        [FromQuery] string? providerName = null,
        [FromQuery] string? coverageType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? expiringInDays = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Getting insurance policies with filters - Provider: {ProviderName}, CoverageType: {CoverageType}, IsActive: {IsActive}, ExpiringInDays: {ExpiringInDays}, Page: {Page}, PageSize: {PageSize}",
                providerName, coverageType, isActive, expiringInDays, page, pageSize);

            // TODO: Implement actual data access when Entity Framework is set up
            var policies = new List<InsurancePolicyDto>();

            return Ok(policies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance policies");
            return StatusCode(500, new { error = "An error occurred while retrieving insurance policies" });
        }
    }

    /// <summary>
    /// Get a specific insurance policy by ID
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <returns>Insurance policy details</returns>
    /// <response code="200">Returns the insurance policy</response>
    /// <response code="404">If the insurance policy is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InsurancePolicyDto), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<InsurancePolicyDto>> GetInsurancePolicy(int id)
    {
        try
        {
            _logger.LogInformation("Getting insurance policy with ID: {PolicyId}", id);

            // TODO: Implement actual data access when Entity Framework is set up
            return NotFound(new { error = "Insurance policy not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance policy {PolicyId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the insurance policy" });
        }
    }

    /// <summary>
    /// Create a new insurance policy
    /// </summary>
    /// <param name="createInsurancePolicyDto">Insurance policy data</param>
    /// <returns>Created insurance policy</returns>
    /// <response code="201">Returns the newly created insurance policy</response>
    /// <response code="400">If the insurance policy data is invalid</response>
    /// <response code="409">If a policy with the same policy number already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(InsurancePolicyDto), 201)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 409)]
    public async Task<ActionResult<InsurancePolicyDto>> CreateInsurancePolicy([FromBody] CreateInsurancePolicyDto createInsurancePolicyDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new insurance policy: {PolicyNumber}", createInsurancePolicyDto.PolicyNumber);

            // TODO: Implement actual data access when Entity Framework is set up
            var createdPolicy = new InsurancePolicyDto
            {
                Id = 1, // Placeholder
                PolicyNumber = createInsurancePolicyDto.PolicyNumber,
                ProviderName = createInsurancePolicyDto.ProviderName,
                ProviderContact = createInsurancePolicyDto.ProviderContact,
                StartDate = createInsurancePolicyDto.StartDate,
                EndDate = createInsurancePolicyDto.EndDate,
                PremiumAmount = createInsurancePolicyDto.PremiumAmount,
                PaymentFrequency = createInsurancePolicyDto.PaymentFrequency,
                Deductible = createInsurancePolicyDto.Deductible,
                CoverageLimit = createInsurancePolicyDto.CoverageLimit,
                CoverageType = createInsurancePolicyDto.CoverageType,
                CoverageDetails = createInsurancePolicyDto.CoverageDetails,
                PolicyDocumentUrl = createInsurancePolicyDto.PolicyDocumentUrl,
                Notes = createInsurancePolicyDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetInsurancePolicy), new { id = createdPolicy.Id }, createdPolicy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating insurance policy");
            return StatusCode(500, new { error = "An error occurred while creating the insurance policy" });
        }
    }

    /// <summary>
    /// Update an existing insurance policy
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <param name="updateInsurancePolicyDto">Updated insurance policy data</param>
    /// <returns>Updated insurance policy</returns>
    /// <response code="200">Returns the updated insurance policy</response>
    /// <response code="400">If the insurance policy data is invalid</response>
    /// <response code="404">If the insurance policy is not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(InsurancePolicyDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<InsurancePolicyDto>> UpdateInsurancePolicy(int id, [FromBody] UpdateInsurancePolicyDto updateInsurancePolicyDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating insurance policy with ID: {PolicyId}", id);

            // TODO: Implement actual data access when Entity Framework is set up
            return NotFound(new { error = "Insurance policy not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating insurance policy {PolicyId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the insurance policy" });
        }
    }

    /// <summary>
    /// Delete an insurance policy
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Insurance policy deleted successfully</response>
    /// <response code="404">If the insurance policy is not found</response>
    /// <response code="409">If the policy has associated vehicles that prevent deletion</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 409)]
    public async Task<IActionResult> DeleteInsurancePolicy(int id)
    {
        try
        {
            _logger.LogInformation("Deleting insurance policy with ID: {PolicyId}", id);

            // TODO: Implement actual data access when Entity Framework is set up
            return NotFound(new { error = "Insurance policy not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting insurance policy {PolicyId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the insurance policy" });
        }
    }

    /// <summary>
    /// Add a vehicle to an insurance policy
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <param name="manageVehicleInsuranceDto">Vehicle insurance data</param>
    /// <returns>Updated insurance policy</returns>
    /// <response code="200">Returns the updated insurance policy</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the insurance policy or vehicle is not found</response>
    /// <response code="409">If the vehicle is already covered by this policy</response>
    [HttpPost("{id}/vehicles")]
    [ProducesResponseType(typeof(InsurancePolicyDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 409)]
    public async Task<ActionResult<InsurancePolicyDto>> AddVehicleToPolicy(int id, [FromBody] ManageVehicleInsuranceDto manageVehicleInsuranceDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Adding vehicle {VehicleId} to insurance policy {PolicyId}", manageVehicleInsuranceDto.VehicleId, id);

            // TODO: Implement actual data access when Entity Framework is set up
            return NotFound(new { error = "Insurance policy not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding vehicle to insurance policy");
            return StatusCode(500, new { error = "An error occurred while adding the vehicle to the insurance policy" });
        }
    }

    /// <summary>
    /// Remove a vehicle from an insurance policy
    /// </summary>
    /// <param name="id">Insurance policy ID</param>
    /// <param name="vehicleId">Vehicle ID to remove</param>
    /// <returns>Updated insurance policy</returns>
    /// <response code="200">Returns the updated insurance policy</response>
    /// <response code="404">If the insurance policy or vehicle insurance relationship is not found</response>
    [HttpDelete("{id}/vehicles/{vehicleId}")]
    [ProducesResponseType(typeof(InsurancePolicyDto), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<InsurancePolicyDto>> RemoveVehicleFromPolicy(int id, int vehicleId)
    {
        try
        {
            _logger.LogInformation("Removing vehicle {VehicleId} from insurance policy {PolicyId}", vehicleId, id);

            // TODO: Implement actual data access when Entity Framework is set up
            return NotFound(new { error = "Insurance policy or vehicle relationship not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing vehicle from insurance policy");
            return StatusCode(500, new { error = "An error occurred while removing the vehicle from the insurance policy" });
        }
    }

    /// <summary>
    /// Get insurance policies for a specific vehicle
    /// </summary>
    /// <param name="vehicleId">Vehicle ID</param>
    /// <param name="includeExpired">Include expired policies (default: false)</param>
    /// <returns>List of insurance policies for the vehicle</returns>
    /// <response code="200">Returns the list of insurance policies</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpGet("vehicle/{vehicleId}")]
    [ProducesResponseType(typeof(IEnumerable<InsurancePolicyDto>), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<IEnumerable<InsurancePolicyDto>>> GetInsurancePoliciesByVehicle(
        int vehicleId,
        [FromQuery] bool includeExpired = false)
    {
        try
        {
            _logger.LogInformation("Getting insurance policies for vehicle {VehicleId}", vehicleId);

            // TODO: Implement actual data access when Entity Framework is set up
            var policies = new List<InsurancePolicyDto>();
            return Ok(policies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance policies for vehicle {VehicleId}", vehicleId);
            return StatusCode(500, new { error = "An error occurred while retrieving insurance policies" });
        }
    }

    /// <summary>
    /// Get insurance statistics
    /// </summary>
    /// <returns>Insurance statistics</returns>
    /// <response code="200">Returns insurance statistics</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult> GetInsuranceStatistics()
    {
        try
        {
            _logger.LogInformation("Getting insurance statistics");

            // TODO: Implement actual data access when Entity Framework is set up
            var statistics = new
            {
                TotalPolicies = 0,
                ActivePolicies = 0,
                ExpiringPolicies = 0,
                TotalVehiclesCovered = 0,
                TotalPremiumAmount = 0.0m,
                AveragePremium = 0.0m,
                TotalCoverageLimit = 0.0m,
                PoliciesByProvider = new Dictionary<string, int>(),
                PoliciesByCoverageType = new Dictionary<string, int>()
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance statistics");
            return StatusCode(500, new { error = "An error occurred while retrieving insurance statistics" });
        }
    }
}
