using FleetAssistant.Shared.DTOs;
using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Repositories;
using FleetAssistant.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FleetAssistant.WebApi.Controllers;

/// <summary>
/// API controller for managing vehicle financial records
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FinancialsController : ControllerBase
{
    private readonly IFinancialRepository _financialRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IStorageService _blobStorageService;
    private readonly ILogger<FinancialsController> _logger;

    public FinancialsController(
        IFinancialRepository financialRepository,
        IVehicleRepository vehicleRepository,
        IStorageService blobStorageService,
        ILogger<FinancialsController> logger)
    {
        _financialRepository = financialRepository;
        _vehicleRepository = vehicleRepository;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all financial records with optional filtering
    /// </summary>
    /// <param name="vehicleId">Filter by vehicle ID</param>
    /// <param name="financialType">Filter by financial type</param>
    /// <param name="providerName">Filter by provider name</param>
    /// <param name="startDate">Filter by start date (inclusive)</param>
    /// <param name="endDate">Filter by end date (inclusive)</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>List of financial records matching the criteria</returns>
    /// <response code="200">Returns the list of financial records</response>
    /// <response code="400">If the request parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VehicleFinancialDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<ActionResult<IEnumerable<VehicleFinancialDto>>> GetFinancialRecords(
        [FromQuery] int? vehicleId = null,
        [FromQuery] FinancialType? financialType = null,
        [FromQuery] string? providerName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Getting financial records with filters - VehicleId: {VehicleId}, Type: {FinancialType}, Provider: {ProviderName}, StartDate: {StartDate}, EndDate: {EndDate}, Page: {Page}, PageSize: {PageSize}",
                vehicleId, financialType, providerName, startDate, endDate, page, pageSize);

            IEnumerable<VehicleFinancial> financialRecords;

            if (vehicleId.HasValue)
            {
                financialRecords = await _financialRepository.GetByVehicleIdAsync(vehicleId.Value, page, pageSize);
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                financialRecords = await _financialRepository.GetByDateRangeAsync(startDate.Value, endDate.Value, page, pageSize);
            }
            else
            {
                financialRecords = await _financialRepository.GetAllAsync();
                financialRecords = financialRecords.Skip((page - 1) * pageSize).Take(pageSize);
            }

            // Apply additional filtering
            if (financialType.HasValue)
            {
                financialRecords = financialRecords.Where(f => f.FinancialType == financialType.Value);
            }

            if (!string.IsNullOrEmpty(providerName))
            {
                financialRecords = financialRecords.Where(f => f.ProviderName != null &&
                    f.ProviderName.Contains(providerName, StringComparison.OrdinalIgnoreCase));
            }

            var financialRecordDtos = financialRecords.Select(MapToDto);
            return Ok(financialRecordDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial records");
            return StatusCode(500, new { error = "An error occurred while retrieving financial records" });
        }
    }

    /// <summary>
    /// Get financial records for a specific vehicle
    /// </summary>
    /// <param name="vehicleId">Vehicle ID</param>
    /// <param name="financialType">Filter by financial type</param>
    /// <param name="startDate">Filter by start date (inclusive)</param>
    /// <param name="endDate">Filter by end date (inclusive)</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>List of financial records for the vehicle</returns>
    /// <response code="200">Returns the list of financial records</response>
    /// <response code="400">If the request parameters are invalid</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpGet("vehicle/{vehicleId}")]
    [ProducesResponseType(typeof(IEnumerable<VehicleFinancialDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<IEnumerable<VehicleFinancialDto>>> GetFinancialRecordsByVehicle(
        int vehicleId,
        [FromQuery] FinancialType? financialType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Getting financial records for vehicle {VehicleId}", vehicleId);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var financialRecords = await _financialRepository.GetByVehicleIdAsync(vehicleId, page, pageSize);

            // Apply additional filtering
            if (financialType.HasValue)
            {
                financialRecords = financialRecords.Where(f => f.FinancialType == financialType.Value);
            }

            if (startDate.HasValue)
            {
                financialRecords = financialRecords.Where(f => f.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                financialRecords = financialRecords.Where(f => f.StartDate <= endDate.Value);
            }

            var financialRecordDtos = financialRecords.Select(MapToDto);
            return Ok(financialRecordDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial records for vehicle {VehicleId}", vehicleId);
            return StatusCode(500, new { error = "An error occurred while retrieving financial records" });
        }
    }

    /// <summary>
    /// Get upcoming payments for all vehicles or a specific vehicle
    /// </summary>
    /// <param name="vehicleId">Vehicle ID (optional, if not provided returns for all vehicles)</param>
    /// <param name="daysAhead">Number of days ahead to look for upcoming payments (default: 30)</param>
    /// <returns>List of upcoming payments</returns>
    /// <response code="200">Returns the list of upcoming payments</response>
    /// <response code="404">If the vehicle is not found (when vehicleId is provided)</response>
    [HttpGet("upcoming-payments")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult> GetUpcomingPayments(
        [FromQuery] int? vehicleId = null,
        [FromQuery] int daysAhead = 30)
    {
        try
        {
            _logger.LogInformation("Getting upcoming payments - VehicleId: {VehicleId}, DaysAhead: {DaysAhead}", vehicleId, daysAhead);

            // If vehicleId is provided, check if vehicle exists
            if (vehicleId.HasValue)
            {
                var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId.Value);
                if (vehicle == null)
                {
                    return NotFound(new { error = "Vehicle not found" });
                }
            }

            // Get financial records with upcoming payment dates
            var targetDate = DateTime.UtcNow.Date.AddDays(daysAhead);
            var allFinancials = await _financialRepository.GetAllAsync();

            var upcomingPayments = allFinancials
                .Where(f => f.NextPaymentDate.HasValue &&
                           f.NextPaymentDate.Value.Date <= targetDate &&
                           f.NextPaymentDate.Value.Date >= DateTime.UtcNow.Date)
                .Where(f => !vehicleId.HasValue || f.VehicleId == vehicleId.Value)
                .Select(f => new
                {
                    f.Id,
                    f.VehicleId,
                    VehicleName = f.Vehicle?.Name,
                    f.FinancialType,
                    f.Amount,
                    f.NextPaymentDate,
                    f.ProviderName,
                    DaysUntilDue = (f.NextPaymentDate!.Value.Date - DateTime.UtcNow.Date).Days
                })
                .OrderBy(p => p.NextPaymentDate);

            return Ok(upcomingPayments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming payments");
            return StatusCode(500, new { error = "An error occurred while retrieving upcoming payments" });
        }
    }

    /// <summary>
    /// Get a specific financial record by ID
    /// </summary>
    /// <param name="id">Financial record ID</param>
    /// <returns>Financial record details</returns>
    /// <response code="200">Returns the financial record</response>
    /// <response code="404">If the financial record is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VehicleFinancialDto), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<VehicleFinancialDto>> GetFinancialRecord(int id)
    {
        try
        {
            _logger.LogInformation("Getting financial record with ID: {FinancialRecordId}", id);

            var financialRecord = await _financialRepository.GetByIdAsync(id);
            if (financialRecord == null)
            {
                return NotFound(new { error = "Financial record not found" });
            }

            var financialRecordDto = MapToDto(financialRecord);
            return Ok(financialRecordDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial record {FinancialRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the financial record" });
        }
    }

    /// <summary>
    /// Create a new financial record
    /// </summary>
    /// <param name="createVehicleFinancialDto">Financial record data</param>
    /// <returns>Created financial record</returns>
    /// <response code="201">Returns the newly created financial record</response>
    /// <response code="400">If the financial record data is invalid</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleFinancialDto), 201)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<VehicleFinancialDto>> CreateFinancialRecord([FromBody] CreateVehicleFinancialDto createVehicleFinancialDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new financial record for vehicle {VehicleId}", createVehicleFinancialDto.VehicleId);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(createVehicleFinancialDto.VehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var financial = MapFromCreateDto(createVehicleFinancialDto);
            var createdFinancial = await _financialRepository.AddAsync(financial);

            var createdFinancialDto = MapToDto(createdFinancial);
            return CreatedAtAction(nameof(GetFinancialRecord), new { id = createdFinancialDto.Id }, createdFinancialDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating financial record");
            return StatusCode(500, new { error = "An error occurred while creating the financial record" });
        }
    }

    /// <summary>
    /// Update an existing financial record
    /// </summary>
    /// <param name="id">Financial record ID</param>
    /// <param name="updateVehicleFinancialDto">Updated financial record data</param>
    /// <returns>Updated financial record</returns>
    /// <response code="200">Returns the updated financial record</response>
    /// <response code="400">If the financial record data is invalid</response>
    /// <response code="404">If the financial record is not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(VehicleFinancialDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<VehicleFinancialDto>> UpdateFinancialRecord(int id, [FromBody] UpdateVehicleFinancialDto updateVehicleFinancialDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating financial record with ID: {FinancialRecordId}", id);

            var existingFinancial = await _financialRepository.GetByIdAsync(id);
            if (existingFinancial == null)
            {
                return NotFound(new { error = "Financial record not found" });
            }

            UpdateFromDto(existingFinancial, updateVehicleFinancialDto);
            var updatedFinancial = await _financialRepository.UpdateAsync(existingFinancial);

            var updatedFinancialDto = MapToDto(updatedFinancial);
            return Ok(updatedFinancialDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating financial record {FinancialRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the financial record" });
        }
    }

    /// <summary>
    /// Delete a financial record
    /// </summary>
    /// <param name="id">Financial record ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Financial record deleted successfully</response>
    /// <response code="404">If the financial record is not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<IActionResult> DeleteFinancialRecord(int id)
    {
        try
        {
            _logger.LogInformation("Deleting financial record with ID: {FinancialRecordId}", id);

            var financialRecord = await _financialRepository.GetByIdAsync(id);
            if (financialRecord == null)
            {
                return NotFound(new { error = "Financial record not found" });
            }

            await _financialRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting financial record {FinancialRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the financial record" });
        }
    }

    /// <summary>
    /// Get financial statistics for a vehicle
    /// </summary>
    /// <param name="vehicleId">Vehicle ID</param>
    /// <param name="startDate">Start date for statistics (optional)</param>
    /// <param name="endDate">End date for statistics (optional)</param>
    /// <returns>Financial statistics</returns>
    /// <response code="200">Returns financial statistics</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpGet("vehicle/{vehicleId}/statistics")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult> GetFinancialStatistics(
        int vehicleId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            _logger.LogInformation("Getting financial statistics for vehicle {VehicleId}", vehicleId);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var statistics = await _financialRepository.GetFinancialStatsAsync(vehicleId);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial statistics for vehicle {VehicleId}", vehicleId);
            return StatusCode(500, new { error = "An error occurred while retrieving financial statistics" });
        }
    }

    /// <summary>
    /// Calculate depreciation for a vehicle
    /// </summary>
    /// <param name="vehicleId">Vehicle ID</param>
    /// <param name="asOfDate">Date to calculate depreciation as of (default: today)</param>
    /// <returns>Depreciation calculation</returns>
    /// <response code="200">Returns depreciation calculation</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpGet("vehicle/{vehicleId}/depreciation")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult> CalculateDepreciation(
        int vehicleId,
        [FromQuery] DateTime? asOfDate = null)
    {
        try
        {
            var calculationDate = asOfDate ?? DateTime.Today;
            _logger.LogInformation("Calculating depreciation for vehicle {VehicleId} as of {AsOfDate}", vehicleId, calculationDate);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            // Get vehicle financial records to find purchase price and depreciation info
            var financialRecords = await _financialRepository.GetByVehicleIdAsync(vehicleId);
            var purchaseRecord = financialRecords.FirstOrDefault(f => f.FinancialType == FinancialType.Purchase);

            if (purchaseRecord == null)
            {
                return BadRequest(new { error = "Vehicle purchase record not found for depreciation calculation" });
            }

            // Simple depreciation calculation (this could be enhanced with more sophisticated methods)
            var purchasePrice = purchaseRecord.PurchasePrice ?? purchaseRecord.Amount;
            var currentValue = purchaseRecord.CurrentValue ?? purchasePrice;
            var totalDepreciation = purchasePrice - currentValue;
            var depreciationPercentage = purchasePrice > 0 ? (totalDepreciation / purchasePrice) * 100 : 0;

            var depreciation = new
            {
                VehicleId = vehicleId,
                AsOfDate = calculationDate,
                PurchasePrice = purchasePrice,
                CurrentValue = currentValue,
                TotalDepreciation = totalDepreciation,
                DepreciationPercentage = depreciationPercentage,
                DepreciationMethod = purchaseRecord.DepreciationMethod?.ToString() ?? "Not specified"
            };

            return Ok(depreciation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating depreciation for vehicle {VehicleId}", vehicleId);
            return StatusCode(500, new { error = "An error occurred while calculating depreciation" });
        }
    }

    /// <summary>
    /// Get fleet-wide financial summary
    /// </summary>
    /// <returns>Fleet financial summary</returns>
    /// <response code="200">Returns fleet financial summary</response>
    [HttpGet("fleet-summary")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult> GetFleetFinancialSummary()
    {
        try
        {
            _logger.LogInformation("Getting fleet financial summary");

            var summary = await _financialRepository.GetFleetFinancialSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fleet financial summary");
            return StatusCode(500, new { error = "An error occurred while retrieving fleet financial summary" });
        }
    }

    /// <summary>
    /// Maps VehicleFinancial entity to VehicleFinancialDto
    /// </summary>
    private static VehicleFinancialDto MapToDto(VehicleFinancial financial)
    {
        return new VehicleFinancialDto
        {
            Id = financial.Id,
            VehicleId = financial.VehicleId,
            VehicleName = financial.Vehicle?.Name,
            FinancialType = financial.FinancialType,
            Amount = financial.Amount,
            PaymentFrequency = financial.PaymentFrequency,
            StartDate = financial.StartDate,
            EndDate = financial.EndDate,
            NextPaymentDate = financial.NextPaymentDate,
            ProviderName = financial.ProviderName,
            AccountNumber = financial.AccountNumber,
            InterestRate = financial.InterestRate,
            RemainingBalance = financial.RemainingBalance,
            PurchasePrice = financial.PurchasePrice,
            CurrentValue = financial.CurrentValue,
            DepreciationMethod = financial.DepreciationMethod,
            DepreciationRate = financial.DepreciationRate,
            DocumentUrl = financial.DocumentUrl,
            Notes = financial.Notes,
            CreatedAt = financial.CreatedAt,
            UpdatedAt = financial.UpdatedAt
        };
    }

    /// <summary>
    /// Maps CreateVehicleFinancialDto to VehicleFinancial entity
    /// </summary>
    private static VehicleFinancial MapFromCreateDto(CreateVehicleFinancialDto dto)
    {
        return new VehicleFinancial
        {
            VehicleId = dto.VehicleId,
            FinancialType = dto.FinancialType,
            Amount = dto.Amount,
            PaymentFrequency = dto.PaymentFrequency,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            NextPaymentDate = dto.NextPaymentDate,
            ProviderName = dto.ProviderName,
            AccountNumber = dto.AccountNumber,
            InterestRate = dto.InterestRate,
            RemainingBalance = dto.RemainingBalance,
            PurchasePrice = dto.PurchasePrice,
            CurrentValue = dto.CurrentValue,
            DepreciationMethod = dto.DepreciationMethod,
            DepreciationRate = dto.DepreciationRate,
            DocumentUrl = dto.DocumentUrl,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates VehicleFinancial entity with values from UpdateVehicleFinancialDto
    /// </summary>
    private static void UpdateFromDto(VehicleFinancial financial, UpdateVehicleFinancialDto dto)
    {
        if (dto.FinancialType.HasValue)
            financial.FinancialType = dto.FinancialType.Value;

        if (dto.Amount.HasValue)
            financial.Amount = dto.Amount.Value;

        if (dto.PaymentFrequency.HasValue)
            financial.PaymentFrequency = dto.PaymentFrequency;

        if (dto.StartDate.HasValue)
            financial.StartDate = dto.StartDate.Value;

        if (dto.EndDate.HasValue)
            financial.EndDate = dto.EndDate;

        if (dto.NextPaymentDate.HasValue)
            financial.NextPaymentDate = dto.NextPaymentDate;

        if (dto.ProviderName != null)
            financial.ProviderName = dto.ProviderName;

        if (dto.AccountNumber != null)
            financial.AccountNumber = dto.AccountNumber;

        if (dto.InterestRate.HasValue)
            financial.InterestRate = dto.InterestRate;

        if (dto.RemainingBalance.HasValue)
            financial.RemainingBalance = dto.RemainingBalance;

        if (dto.PurchasePrice.HasValue)
            financial.PurchasePrice = dto.PurchasePrice;

        if (dto.CurrentValue.HasValue)
            financial.CurrentValue = dto.CurrentValue;

        if (dto.DepreciationMethod.HasValue)
            financial.DepreciationMethod = dto.DepreciationMethod;

        if (dto.DepreciationRate.HasValue)
            financial.DepreciationRate = dto.DepreciationRate;

        if (dto.DocumentUrl != null)
            financial.DocumentUrl = dto.DocumentUrl;

        if (dto.Notes != null)
            financial.Notes = dto.Notes;

        financial.UpdatedAt = DateTime.UtcNow;
    }
}
