using FleetAssistant.Shared.DTOs;
using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Repositories;
using FleetAssistant.WebApi.Services;
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
    private readonly IInsuranceRepository _insuranceRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IStorageService _blobStorageService;
    private readonly ILogger<InsuranceController> _logger;

    public InsuranceController(
        IInsuranceRepository insuranceRepository,
        IVehicleRepository vehicleRepository,
        IStorageService blobStorageService,
        ILogger<InsuranceController> logger)
    {
        _insuranceRepository = insuranceRepository;
        _vehicleRepository = vehicleRepository;
        _blobStorageService = blobStorageService;
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

            IEnumerable<InsurancePolicy> policies;

            if (!string.IsNullOrEmpty(providerName))
            {
                policies = await _insuranceRepository.GetByProviderAsync(providerName, page, pageSize);
            }
            else if (!string.IsNullOrEmpty(coverageType))
            {
                policies = await _insuranceRepository.GetByCoverageTypeAsync(coverageType, page, pageSize);
            }
            else if (isActive.HasValue && isActive.Value)
            {
                policies = await _insuranceRepository.GetActiveAsync(page, pageSize);
            }
            else if (expiringInDays.HasValue)
            {
                policies = await _insuranceRepository.GetExpiringAsync(expiringInDays.Value, page, pageSize);
            }
            else
            {
                policies = await _insuranceRepository.GetAllAsync();
            }

            var policyDtos = policies.Select(p => MapToDto(p));
            return Ok(policyDtos);
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

            var policy = await _insuranceRepository.GetByIdAsync(id);
            if (policy == null)
            {
                return NotFound(new { error = "Insurance policy not found" });
            }

            var policyDto = MapToDto(policy);
            return Ok(policyDto);
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

            // Check if policy number already exists
            var existingPolicy = await _insuranceRepository.GetByPolicyNumberAsync(createInsurancePolicyDto.PolicyNumber);
            if (existingPolicy != null)
            {
                return Conflict(new { error = "A policy with this policy number already exists" });
            }

            var policy = MapFromCreateDto(createInsurancePolicyDto);
            var createdPolicy = await _insuranceRepository.AddAsync(policy);

            var createdPolicyDto = MapToDto(createdPolicy);
            return CreatedAtAction(nameof(GetInsurancePolicy), new { id = createdPolicyDto.Id }, createdPolicyDto);
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

            var existingPolicy = await _insuranceRepository.GetByIdAsync(id);
            if (existingPolicy == null)
            {
                return NotFound(new { error = "Insurance policy not found" });
            }

            // Check if updating policy number would create a conflict
            if (!string.IsNullOrEmpty(updateInsurancePolicyDto.PolicyNumber) &&
                updateInsurancePolicyDto.PolicyNumber != existingPolicy.PolicyNumber)
            {
                var duplicatePolicy = await _insuranceRepository.GetByPolicyNumberAsync(updateInsurancePolicyDto.PolicyNumber);
                if (duplicatePolicy != null)
                {
                    return Conflict(new { error = "A policy with this policy number already exists" });
                }
            }

            UpdateFromDto(existingPolicy, updateInsurancePolicyDto);
            var updatedPolicy = await _insuranceRepository.UpdateAsync(existingPolicy);

            var updatedPolicyDto = MapToDto(updatedPolicy);
            return Ok(updatedPolicyDto);
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

            var policy = await _insuranceRepository.GetByIdAsync(id);
            if (policy == null)
            {
                return NotFound(new { error = "Insurance policy not found" });
            }

            // Check if policy has active vehicle insurances
            if (policy.VehicleInsurances?.Any(vi => vi.IsActive) == true)
            {
                return Conflict(new { error = "Cannot delete policy with active vehicle insurances. Remove vehicles first." });
            }

            await _insuranceRepository.DeleteAsync(id);

            return NoContent();
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

            // Check if policy exists
            var policy = await _insuranceRepository.GetByIdAsync(id);
            if (policy == null)
            {
                return NotFound(new { error = "Insurance policy not found" });
            }

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(manageVehicleInsuranceDto.VehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            // Check if vehicle is already covered by this policy
            if (policy.VehicleInsurances?.Any(vi => vi.VehicleId == manageVehicleInsuranceDto.VehicleId && vi.IsActive) == true)
            {
                return Conflict(new { error = "Vehicle is already covered by this policy" });
            }

            // Add vehicle to policy (this would need to be implemented in the repository)
            // For now, this is a simplified implementation
            return StatusCode(501, new { error = "Vehicle-policy management not fully implemented yet" });
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

            // Check if policy exists
            var policy = await _insuranceRepository.GetByIdAsync(id);
            if (policy == null)
            {
                return NotFound(new { error = "Insurance policy not found" });
            }

            // Check if vehicle is covered by this policy
            var vehicleInsurance = policy.VehicleInsurances?.FirstOrDefault(vi => vi.VehicleId == vehicleId && vi.IsActive);
            if (vehicleInsurance == null)
            {
                return NotFound(new { error = "Vehicle is not covered by this policy or relationship not found" });
            }

            // Remove vehicle from policy (this would need to be implemented in the repository)
            // For now, this is a simplified implementation
            return NotFound(new { error = "Vehicle-policy management not fully implemented yet" });
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

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var policies = await _insuranceRepository.GetByVehicleIdAsync(vehicleId);

            // Filter out expired policies if not requested
            if (!includeExpired)
            {
                var today = DateTime.UtcNow.Date;
                policies = policies.Where(p => p.EndDate >= today);
            }

            var policyDtos = policies.Select(p => MapToDto(p));
            return Ok(policyDtos);
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

            var statistics = await _insuranceRepository.GetInsuranceStatsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance statistics");
            return StatusCode(500, new { error = "An error occurred while retrieving insurance statistics" });
        }
    }

    /// <summary>
    /// Maps InsurancePolicy entity to InsurancePolicyDto
    /// </summary>
    private static InsurancePolicyDto MapToDto(InsurancePolicy policy)
    {
        return new InsurancePolicyDto
        {
            Id = policy.Id,
            PolicyNumber = policy.PolicyNumber,
            ProviderName = policy.ProviderName,
            ProviderContact = policy.ProviderContact,
            StartDate = policy.StartDate,
            EndDate = policy.EndDate,
            PremiumAmount = policy.PremiumAmount,
            PaymentFrequency = policy.PaymentFrequency,
            Deductible = policy.Deductible,
            CoverageLimit = policy.CoverageLimit,
            CoverageType = policy.CoverageType,
            CoverageDetails = policy.CoverageDetails,
            PolicyDocumentUrl = policy.PolicyDocumentUrl,
            Notes = policy.Notes,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt,
            CoveredVehicles = policy.VehicleInsurances?.Select(vi => new VehicleInsuranceDto
            {
                Id = vi.Id,
                VehicleId = vi.VehicleId,
                VehicleName = vi.Vehicle?.Name,
                VehicleVin = vi.Vehicle?.Vin,
                StartDate = vi.StartDate,
                EndDate = vi.EndDate,
                IsActive = vi.IsActive
            }).ToList() ?? new List<VehicleInsuranceDto>()
        };
    }

    /// <summary>
    /// Maps CreateInsurancePolicyDto to InsurancePolicy entity
    /// </summary>
    private static InsurancePolicy MapFromCreateDto(CreateInsurancePolicyDto dto)
    {
        return new InsurancePolicy
        {
            PolicyNumber = dto.PolicyNumber,
            ProviderName = dto.ProviderName,
            ProviderContact = dto.ProviderContact,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            PremiumAmount = dto.PremiumAmount,
            PaymentFrequency = dto.PaymentFrequency,
            Deductible = dto.Deductible,
            CoverageLimit = dto.CoverageLimit,
            CoverageType = dto.CoverageType,
            CoverageDetails = dto.CoverageDetails,
            PolicyDocumentUrl = dto.PolicyDocumentUrl,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates InsurancePolicy entity with values from UpdateInsurancePolicyDto
    /// </summary>
    private static void UpdateFromDto(InsurancePolicy policy, UpdateInsurancePolicyDto dto)
    {
        if (!string.IsNullOrEmpty(dto.PolicyNumber))
            policy.PolicyNumber = dto.PolicyNumber;

        if (!string.IsNullOrEmpty(dto.ProviderName))
            policy.ProviderName = dto.ProviderName;

        if (dto.ProviderContact != null)
            policy.ProviderContact = dto.ProviderContact;

        if (dto.StartDate.HasValue)
            policy.StartDate = dto.StartDate.Value;

        if (dto.EndDate.HasValue)
            policy.EndDate = dto.EndDate.Value;

        if (dto.PremiumAmount.HasValue)
            policy.PremiumAmount = dto.PremiumAmount.Value;

        if (dto.PaymentFrequency.HasValue)
            policy.PaymentFrequency = dto.PaymentFrequency.Value;

        if (dto.Deductible.HasValue)
            policy.Deductible = dto.Deductible.Value;

        if (dto.CoverageLimit.HasValue)
            policy.CoverageLimit = dto.CoverageLimit.Value;

        if (dto.CoverageType.HasValue)
            policy.CoverageType = dto.CoverageType.Value;

        if (dto.CoverageDetails != null)
            policy.CoverageDetails = dto.CoverageDetails;

        if (dto.PolicyDocumentUrl != null)
            policy.PolicyDocumentUrl = dto.PolicyDocumentUrl;

        if (dto.Notes != null)
            policy.Notes = dto.Notes;

        policy.UpdatedAt = DateTime.UtcNow;
    }
}
