using FleetAssistant.Shared.DTOs;
using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Repositories.Interfaces;
using FleetAssistant.WebApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FleetAssistant.WebApi.Controllers;

/// <summary>
/// API controller for managing fuel logs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FuelLogsController : ControllerBase
{
    private readonly IFuelLogRepository _fuelLogRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IStorageService _blobStorageService;
    private readonly ILogger<FuelLogsController> _logger;

    public FuelLogsController(
        IFuelLogRepository fuelLogRepository,
        IVehicleRepository vehicleRepository,
        IStorageService blobStorageService,
        ILogger<FuelLogsController> logger)
    {
        _fuelLogRepository = fuelLogRepository;
        _vehicleRepository = vehicleRepository;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all fuel logs with optional filtering
    /// </summary>
    /// <param name="vehicleId">Filter by vehicle ID</param>
    /// <param name="startDate">Filter by start date (inclusive)</param>
    /// <param name="endDate">Filter by end date (inclusive)</param>
    /// <param name="fuelType">Filter by fuel type</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>List of fuel logs matching the criteria</returns>
    /// <response code="200">Returns the list of fuel logs</response>
    /// <response code="400">If the request parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FuelLogDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<ActionResult<IEnumerable<FuelLogDto>>> GetFuelLogs(
        [FromQuery] int? vehicleId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? fuelType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Getting fuel logs with filters - VehicleId: {VehicleId}, StartDate: {StartDate}, EndDate: {EndDate}, FuelType: {FuelType}, Page: {Page}, PageSize: {PageSize}",
                vehicleId, startDate, endDate, fuelType, page, pageSize);

            IEnumerable<FuelLog> fuelLogs;

            if (vehicleId.HasValue)
            {
                fuelLogs = await _fuelLogRepository.GetByVehicleIdAsync(vehicleId.Value, page, pageSize);
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                fuelLogs = await _fuelLogRepository.GetByDateRangeAsync(startDate.Value, endDate.Value, vehicleId, page, pageSize);
            }
            else
            {
                fuelLogs = await _fuelLogRepository.GetAllAsync();
                fuelLogs = fuelLogs.Skip((page - 1) * pageSize).Take(pageSize);
            }

            // Apply additional filtering
            if (!string.IsNullOrEmpty(fuelType))
            {
                fuelLogs = fuelLogs.Where(f => f.FuelType != null &&
                    f.FuelType.Contains(fuelType, StringComparison.OrdinalIgnoreCase));
            }

            var fuelLogDtos = fuelLogs.Select(MapToDto);
            return Ok(fuelLogDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fuel logs");
            return StatusCode(500, new { error = "An error occurred while retrieving fuel logs" });
        }
    }

    /// <summary>
    /// Get fuel logs for a specific vehicle
    /// </summary>
    /// <param name="vehicleId">Vehicle ID</param>
    /// <param name="startDate">Filter by start date (inclusive)</param>
    /// <param name="endDate">Filter by end date (inclusive)</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>List of fuel logs for the vehicle</returns>
    /// <response code="200">Returns the list of fuel logs</response>
    /// <response code="400">If the request parameters are invalid</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpGet("vehicle/{vehicleId}")]
    [ProducesResponseType(typeof(IEnumerable<FuelLogDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<IEnumerable<FuelLogDto>>> GetFuelLogsByVehicle(
        int vehicleId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Getting fuel logs for vehicle {VehicleId}", vehicleId);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            IEnumerable<FuelLog> fuelLogs;

            if (startDate.HasValue && endDate.HasValue)
            {
                fuelLogs = await _fuelLogRepository.GetByDateRangeAsync(startDate.Value, endDate.Value, vehicleId, page, pageSize);
            }
            else
            {
                fuelLogs = await _fuelLogRepository.GetByVehicleIdAsync(vehicleId, page, pageSize);
            }

            var fuelLogDtos = fuelLogs.Select(MapToDto);
            return Ok(fuelLogDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fuel logs for vehicle {VehicleId}", vehicleId);
            return StatusCode(500, new { error = "An error occurred while retrieving fuel logs" });
        }
    }

    /// <summary>
    /// Get a specific fuel log by ID
    /// </summary>
    /// <param name="id">Fuel log ID</param>
    /// <returns>Fuel log details</returns>
    /// <response code="200">Returns the fuel log</response>
    /// <response code="404">If the fuel log is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FuelLogDto), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<FuelLogDto>> GetFuelLog(int id)
    {
        try
        {
            _logger.LogInformation("Getting fuel log with ID: {FuelLogId}", id);

            var fuelLog = await _fuelLogRepository.GetByIdAsync(id);
            if (fuelLog == null)
            {
                return NotFound(new { error = "Fuel log not found" });
            }

            var fuelLogDto = MapToDto(fuelLog);
            return Ok(fuelLogDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fuel log {FuelLogId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the fuel log" });
        }
    }

    /// <summary>
    /// Create a new fuel log entry
    /// </summary>
    /// <param name="createFuelLogDto">Fuel log data</param>
    /// <returns>Created fuel log</returns>
    /// <response code="201">Returns the newly created fuel log</response>
    /// <response code="400">If the fuel log data is invalid</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(FuelLogDto), 201)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<FuelLogDto>> CreateFuelLog([FromBody] CreateFuelLogDto createFuelLogDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new fuel log for vehicle {VehicleId}", createFuelLogDto.VehicleId);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(createFuelLogDto.VehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var fuelLog = MapFromCreateDto(createFuelLogDto);

            // Calculate MPG and save the fuel log
            var createdFuelLog = await _fuelLogRepository.CalculateAndUpdateMpgAsync(fuelLog);

            var createdFuelLogDto = MapToDto(createdFuelLog);
            return CreatedAtAction(nameof(GetFuelLog), new { id = createdFuelLogDto.Id }, createdFuelLogDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fuel log");
            return StatusCode(500, new { error = "An error occurred while creating the fuel log" });
        }
    }

    /// <summary>
    /// Update an existing fuel log
    /// </summary>
    /// <param name="id">Fuel log ID</param>
    /// <param name="updateFuelLogDto">Updated fuel log data</param>
    /// <returns>Updated fuel log</returns>
    /// <response code="200">Returns the updated fuel log</response>
    /// <response code="400">If the fuel log data is invalid</response>
    /// <response code="404">If the fuel log is not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(FuelLogDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<FuelLogDto>> UpdateFuelLog(int id, [FromBody] UpdateFuelLogDto updateFuelLogDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating fuel log with ID: {FuelLogId}", id);

            var existingFuelLog = await _fuelLogRepository.GetByIdAsync(id);
            if (existingFuelLog == null)
            {
                return NotFound(new { error = "Fuel log not found" });
            }

            UpdateFromDto(existingFuelLog, updateFuelLogDto);

            // Recalculate MPG if relevant fields changed
            var updatedFuelLog = await _fuelLogRepository.CalculateAndUpdateMpgAsync(existingFuelLog);

            var updatedFuelLogDto = MapToDto(updatedFuelLog);
            return Ok(updatedFuelLogDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fuel log {FuelLogId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the fuel log" });
        }
    }

    /// <summary>
    /// Delete a fuel log
    /// </summary>
    /// <param name="id">Fuel log ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Fuel log deleted successfully</response>
    /// <response code="404">If the fuel log is not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<IActionResult> DeleteFuelLog(int id)
    {
        try
        {
            _logger.LogInformation("Deleting fuel log with ID: {FuelLogId}", id);

            var fuelLog = await _fuelLogRepository.GetByIdAsync(id);
            if (fuelLog == null)
            {
                return NotFound(new { error = "Fuel log not found" });
            }

            await _fuelLogRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fuel log {FuelLogId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the fuel log" });
        }
    }

    /// <summary>
    /// Get fuel efficiency statistics for a vehicle
    /// </summary>
    /// <param name="vehicleId">Vehicle ID</param>
    /// <param name="startDate">Start date for statistics (optional)</param>
    /// <param name="endDate">End date for statistics (optional)</param>
    /// <returns>Fuel efficiency statistics</returns>
    /// <response code="200">Returns fuel efficiency statistics</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpGet("vehicle/{vehicleId}/statistics")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult> GetFuelStatistics(
        int vehicleId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            _logger.LogInformation("Getting fuel statistics for vehicle {VehicleId}", vehicleId);

            // Check if vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var statistics = await _fuelLogRepository.GetVehicleStatisticsAsync(vehicleId, startDate, endDate);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fuel statistics for vehicle {VehicleId}", vehicleId);
            return StatusCode(500, new { error = "An error occurred while retrieving fuel statistics" });
        }
    }

    /// <summary>
    /// Maps FuelLog entity to FuelLogDto
    /// </summary>
    private static FuelLogDto MapToDto(FuelLog fuelLog)
    {
        return new FuelLogDto
        {
            Id = fuelLog.Id,
            VehicleId = fuelLog.VehicleId,
            VehicleName = fuelLog.Vehicle?.Name,
            FuelDate = fuelLog.FuelDate,
            OdometerReading = fuelLog.OdometerReading,
            Gallons = fuelLog.Gallons,
            PricePerGallon = fuelLog.PricePerGallon,
            TotalCost = fuelLog.TotalCost,
            Location = fuelLog.Location,
            FuelType = fuelLog.FuelType,
            MilesPerGallon = fuelLog.MilesPerGallon,
            MilesDriven = fuelLog.MilesDriven,
            Notes = fuelLog.Notes,
            CreatedAt = fuelLog.CreatedAt,
            UpdatedAt = fuelLog.UpdatedAt
        };
    }

    /// <summary>
    /// Maps CreateFuelLogDto to FuelLog entity
    /// </summary>
    private static FuelLog MapFromCreateDto(CreateFuelLogDto dto)
    {
        return new FuelLog
        {
            VehicleId = dto.VehicleId,
            FuelDate = dto.FuelDate,
            OdometerReading = dto.OdometerReading,
            Gallons = dto.Gallons,
            PricePerGallon = dto.PricePerGallon,
            TotalCost = dto.TotalCost,
            Location = dto.Location,
            FuelType = dto.FuelType,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates FuelLog entity with values from UpdateFuelLogDto
    /// </summary>
    private static void UpdateFromDto(FuelLog fuelLog, UpdateFuelLogDto dto)
    {
        if (dto.FuelDate.HasValue)
            fuelLog.FuelDate = dto.FuelDate.Value;

        if (dto.OdometerReading.HasValue)
            fuelLog.OdometerReading = dto.OdometerReading.Value;

        if (dto.Gallons.HasValue)
            fuelLog.Gallons = dto.Gallons.Value;

        if (dto.PricePerGallon.HasValue)
            fuelLog.PricePerGallon = dto.PricePerGallon.Value;

        if (dto.TotalCost.HasValue)
            fuelLog.TotalCost = dto.TotalCost.Value;

        if (dto.Location != null)
            fuelLog.Location = dto.Location;

        if (dto.FuelType != null)
            fuelLog.FuelType = dto.FuelType;

        if (dto.Notes != null)
            fuelLog.Notes = dto.Notes;

        fuelLog.UpdatedAt = DateTime.UtcNow;
    }
}
