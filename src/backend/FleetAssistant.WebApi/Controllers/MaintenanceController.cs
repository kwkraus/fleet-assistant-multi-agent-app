using FleetAssistant.Shared.DTOs;
using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Repositories;
using FleetAssistant.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FleetAssistant.WebApi.Controllers;

/// <summary>
/// API controller for managing vehicle maintenance records
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IStorageService _blobStorageService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IMaintenanceRepository maintenanceRepository,
        IVehicleRepository vehicleRepository,
        IStorageService blobStorageService,
        ILogger<MaintenanceController> logger)
    {
        _maintenanceRepository = maintenanceRepository;
        _vehicleRepository = vehicleRepository;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all maintenance records with optional filtering
    /// </summary>
    /// <param name="vehicleId">Filter by vehicle ID</param>
    /// <param name="maintenanceType">Filter by maintenance type</param>
    /// <param name="startDate">Filter by start date (inclusive)</param>
    /// <param name="endDate">Filter by end date (inclusive)</param>
    /// <param name="serviceProvider">Filter by service provider</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>List of maintenance records matching the criteria</returns>
    /// <response code="200">Returns the list of maintenance records</response>
    /// <response code="400">If the request parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceRecordDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<ActionResult<IEnumerable<MaintenanceRecordDto>>> GetMaintenanceRecords(
        [FromQuery] int? vehicleId = null,
        [FromQuery] MaintenanceType? maintenanceType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? serviceProvider = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Getting maintenance records with filters - VehicleId: {VehicleId}, Type: {MaintenanceType}, StartDate: {StartDate}, EndDate: {EndDate}, ServiceProvider: {ServiceProvider}, Page: {Page}, PageSize: {PageSize}",
                vehicleId, maintenanceType, startDate, endDate, serviceProvider, page, pageSize);

            IEnumerable<MaintenanceRecord> records;

            if (vehicleId.HasValue)
            {
                records = await _maintenanceRepository.GetByVehicleIdAsync(vehicleId.Value, page, pageSize);
            }
            else if (maintenanceType.HasValue)
            {
                records = await _maintenanceRepository.GetByTypeAsync(maintenanceType.Value.ToString(), page, pageSize);
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                records = await _maintenanceRepository.GetByDateRangeAsync(startDate.Value, endDate.Value, page, pageSize);
            }
            else
            {
                records = await _maintenanceRepository.GetAllAsync();
            }

            // Apply additional filtering if needed
            if (!string.IsNullOrEmpty(serviceProvider))
            {
                records = records.Where(r => r.ServiceProvider != null &&
                    r.ServiceProvider.Contains(serviceProvider, StringComparison.OrdinalIgnoreCase));
            }

            var maintenanceRecordDtos = records.Select(r => MapToDto(r));
            return Ok(maintenanceRecordDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance records");
            return StatusCode(500, new { error = "An error occurred while retrieving maintenance records" });
        }
    }

    /// <summary>
    /// Get maintenance records for a specific vehicle
    /// </summary>
    /// <param name="vehicleId">Vehicle ID</param>
    /// <param name="maintenanceType">Filter by maintenance type</param>
    /// <param name="startDate">Filter by start date (inclusive)</param>
    /// <param name="endDate">Filter by end date (inclusive)</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>List of maintenance records for the vehicle</returns>
    /// <response code="200">Returns the list of maintenance records</response>
    /// <response code="400">If the request parameters are invalid</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpGet("vehicle/{vehicleId}")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceRecordDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<IEnumerable<MaintenanceRecordDto>>> GetMaintenanceRecordsByVehicle(
        int vehicleId,
        [FromQuery] MaintenanceType? maintenanceType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Getting maintenance records for vehicle {VehicleId}", vehicleId);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var records = await _maintenanceRepository.GetByVehicleIdAsync(vehicleId, page, pageSize);

            // Apply additional filtering
            if (maintenanceType.HasValue)
            {
                records = records.Where(r => r.MaintenanceType == maintenanceType.Value);
            }

            if (startDate.HasValue)
            {
                records = records.Where(r => r.MaintenanceDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                records = records.Where(r => r.MaintenanceDate <= endDate.Value);
            }

            var maintenanceRecordDtos = records.Select(r => MapToDto(r));
            return Ok(maintenanceRecordDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance records for vehicle {VehicleId}", vehicleId);
            return StatusCode(500, new { error = "An error occurred while retrieving maintenance records" });
        }
    }

    /// <summary>
    /// Get upcoming maintenance for a specific vehicle or all vehicles
    /// </summary>
    /// <param name="vehicleId">Vehicle ID (optional, if not provided returns for all vehicles)</param>
    /// <param name="daysAhead">Number of days ahead to look for upcoming maintenance (default: 30)</param>
    /// <returns>List of upcoming maintenance items</returns>
    /// <response code="200">Returns the list of upcoming maintenance</response>
    /// <response code="404">If the vehicle is not found (when vehicleId is provided)</response>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult> GetUpcomingMaintenance(
        [FromQuery] int? vehicleId = null,
        [FromQuery] int daysAhead = 30)
    {
        try
        {
            _logger.LogInformation("Getting upcoming maintenance - VehicleId: {VehicleId}, DaysAhead: {DaysAhead}", vehicleId, daysAhead);

            // If vehicleId is provided, check if vehicle exists
            if (vehicleId.HasValue)
            {
                var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId.Value);
                if (vehicle == null)
                {
                    return NotFound(new { error = "Vehicle not found" });
                }
            }

            var upcomingRecords = await _maintenanceRepository.GetUpcomingMaintenanceAsync();

            // Filter by vehicle if specified
            if (vehicleId.HasValue)
            {
                upcomingRecords = upcomingRecords.Where(r => r.VehicleId == vehicleId.Value);
            }

            // Filter by days ahead
            var targetDate = DateTime.UtcNow.Date.AddDays(daysAhead);
            upcomingRecords = upcomingRecords.Where(r =>
                r.NextMaintenanceDate.HasValue &&
                r.NextMaintenanceDate.Value.Date <= targetDate);

            var upcomingMaintenance = upcomingRecords.Select(r => new
            {
                r.Id,
                r.VehicleId,
                VehicleName = r.Vehicle?.Name,
                r.MaintenanceType,
                LastMaintenanceDate = r.MaintenanceDate,
                NextMaintenanceDate = r.NextMaintenanceDate,
                NextMaintenanceOdometer = r.NextMaintenanceOdometer,
                r.ServiceProvider,
                DaysUntilDue = r.NextMaintenanceDate.HasValue ?
                    (r.NextMaintenanceDate.Value.Date - DateTime.UtcNow.Date).Days : 0
            });

            return Ok(upcomingMaintenance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming maintenance");
            return StatusCode(500, new { error = "An error occurred while retrieving upcoming maintenance" });
        }
    }

    /// <summary>
    /// Get a specific maintenance record by ID
    /// </summary>
    /// <param name="id">Maintenance record ID</param>
    /// <returns>Maintenance record details</returns>
    /// <response code="200">Returns the maintenance record</response>
    /// <response code="404">If the maintenance record is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MaintenanceRecordDto), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<MaintenanceRecordDto>> GetMaintenanceRecord(int id)
    {
        try
        {
            _logger.LogInformation("Getting maintenance record with ID: {MaintenanceRecordId}", id);

            var record = await _maintenanceRepository.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound(new { error = "Maintenance record not found" });
            }

            var recordDto = MapToDto(record);
            return Ok(recordDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance record {MaintenanceRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the maintenance record" });
        }
    }

    /// <summary>
    /// Create a new maintenance record
    /// </summary>
    /// <param name="createMaintenanceRecordDto">Maintenance record data</param>
    /// <returns>Created maintenance record</returns>
    /// <response code="201">Returns the newly created maintenance record</response>
    /// <response code="400">If the maintenance record data is invalid</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceRecordDto), 201)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<MaintenanceRecordDto>> CreateMaintenanceRecord([FromBody] CreateMaintenanceRecordDto createMaintenanceRecordDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new maintenance record for vehicle {VehicleId}", createMaintenanceRecordDto.VehicleId);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(createMaintenanceRecordDto.VehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var record = MapFromCreateDto(createMaintenanceRecordDto);
            var createdRecord = await _maintenanceRepository.AddAsync(record);

            var createdRecordDto = MapToDto(createdRecord);
            return CreatedAtAction(nameof(GetMaintenanceRecord), new { id = createdRecordDto.Id }, createdRecordDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance record");
            return StatusCode(500, new { error = "An error occurred while creating the maintenance record" });
        }
    }

    /// <summary>
    /// Update an existing maintenance record
    /// </summary>
    /// <param name="id">Maintenance record ID</param>
    /// <param name="updateMaintenanceRecordDto">Updated maintenance record data</param>
    /// <returns>Updated maintenance record</returns>
    /// <response code="200">Returns the updated maintenance record</response>
    /// <response code="400">If the maintenance record data is invalid</response>
    /// <response code="404">If the maintenance record is not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(MaintenanceRecordDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<MaintenanceRecordDto>> UpdateMaintenanceRecord(int id, [FromBody] UpdateMaintenanceRecordDto updateMaintenanceRecordDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating maintenance record with ID: {MaintenanceRecordId}", id);

            var existingRecord = await _maintenanceRepository.GetByIdAsync(id);
            if (existingRecord == null)
            {
                return NotFound(new { error = "Maintenance record not found" });
            }

            UpdateFromDto(existingRecord, updateMaintenanceRecordDto);
            var updatedRecord = await _maintenanceRepository.UpdateAsync(existingRecord);

            var updatedRecordDto = MapToDto(updatedRecord);
            return Ok(updatedRecordDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maintenance record {MaintenanceRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the maintenance record" });
        }
    }

    /// <summary>
    /// Delete a maintenance record
    /// </summary>
    /// <param name="id">Maintenance record ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Maintenance record deleted successfully</response>
    /// <response code="404">If the maintenance record is not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<IActionResult> DeleteMaintenanceRecord(int id)
    {
        try
        {
            _logger.LogInformation("Deleting maintenance record with ID: {MaintenanceRecordId}", id);

            var record = await _maintenanceRepository.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound(new { error = "Maintenance record not found" });
            }

            await _maintenanceRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting maintenance record {MaintenanceRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the maintenance record" });
        }
    }

    /// <summary>
    /// Get maintenance statistics for a vehicle
    /// </summary>
    /// <param name="vehicleId">Vehicle ID</param>
    /// <param name="startDate">Start date for statistics (optional)</param>
    /// <param name="endDate">End date for statistics (optional)</param>
    /// <returns>Maintenance statistics</returns>
    /// <response code="200">Returns maintenance statistics</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpGet("vehicle/{vehicleId}/statistics")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult> GetMaintenanceStatistics(
        int vehicleId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            _logger.LogInformation("Getting maintenance statistics for vehicle {VehicleId}", vehicleId);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var statistics = await _maintenanceRepository.GetMaintenanceStatsAsync(vehicleId);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance statistics for vehicle {VehicleId}", vehicleId);
            return StatusCode(500, new { error = "An error occurred while retrieving maintenance statistics" });
        }
    }

    /// <summary>
    /// Maps MaintenanceRecord entity to MaintenanceRecordDto
    /// </summary>
    private static MaintenanceRecordDto MapToDto(MaintenanceRecord record)
    {
        return new MaintenanceRecordDto
        {
            Id = record.Id,
            VehicleId = record.VehicleId,
            VehicleName = record.Vehicle?.Name,
            MaintenanceType = record.MaintenanceType,
            MaintenanceDate = record.MaintenanceDate,
            OdometerReading = record.OdometerReading,
            Description = record.Description,
            Cost = record.Cost,
            ServiceProvider = record.ServiceProvider,
            ServiceProviderContact = record.ServiceProviderContact,
            InvoiceNumber = record.InvoiceNumber,
            WarrantyInfo = record.WarrantyInfo,
            NextMaintenanceDate = record.NextMaintenanceDate,
            NextMaintenanceOdometer = record.NextMaintenanceOdometer,
            Notes = record.Notes,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    /// <summary>
    /// Maps CreateMaintenanceRecordDto to MaintenanceRecord entity
    /// </summary>
    private static MaintenanceRecord MapFromCreateDto(CreateMaintenanceRecordDto dto)
    {
        return new MaintenanceRecord
        {
            VehicleId = dto.VehicleId,
            MaintenanceType = dto.MaintenanceType,
            MaintenanceDate = dto.MaintenanceDate,
            OdometerReading = dto.OdometerReading,
            Description = dto.Description,
            Cost = dto.Cost,
            ServiceProvider = dto.ServiceProvider,
            ServiceProviderContact = dto.ServiceProviderContact,
            InvoiceNumber = dto.InvoiceNumber,
            WarrantyInfo = dto.WarrantyInfo,
            NextMaintenanceDate = dto.NextMaintenanceDate,
            NextMaintenanceOdometer = dto.NextMaintenanceOdometer,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates MaintenanceRecord entity with values from UpdateMaintenanceRecordDto
    /// </summary>
    private static void UpdateFromDto(MaintenanceRecord record, UpdateMaintenanceRecordDto dto)
    {
        if (dto.MaintenanceType.HasValue)
            record.MaintenanceType = dto.MaintenanceType.Value;

        if (dto.MaintenanceDate.HasValue)
            record.MaintenanceDate = dto.MaintenanceDate.Value;

        if (dto.OdometerReading.HasValue)
            record.OdometerReading = dto.OdometerReading.Value;

        if (!string.IsNullOrEmpty(dto.Description))
            record.Description = dto.Description;

        if (dto.Cost.HasValue)
            record.Cost = dto.Cost.Value;

        if (dto.ServiceProvider != null)
            record.ServiceProvider = dto.ServiceProvider;

        if (dto.ServiceProviderContact != null)
            record.ServiceProviderContact = dto.ServiceProviderContact;

        if (dto.InvoiceNumber != null)
            record.InvoiceNumber = dto.InvoiceNumber;

        if (dto.WarrantyInfo != null)
            record.WarrantyInfo = dto.WarrantyInfo;

        if (dto.NextMaintenanceDate.HasValue)
            record.NextMaintenanceDate = dto.NextMaintenanceDate;

        if (dto.NextMaintenanceOdometer.HasValue)
            record.NextMaintenanceOdometer = dto.NextMaintenanceOdometer;

        if (dto.Notes != null)
            record.Notes = dto.Notes;

        record.UpdatedAt = DateTime.UtcNow;
    }
}
