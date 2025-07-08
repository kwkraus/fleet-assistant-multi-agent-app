using FleetAssistant.Shared.DTOs;
using FleetAssistant.Shared.Models;
using FleetAssistant.WebApi.Repositories;
using FleetAssistant.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FleetAssistant.WebApi.Controllers;

/// <summary>
/// API controller for managing fleet vehicles
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IStorageService _blobStorageService;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(
        IVehicleRepository vehicleRepository,
        IStorageService blobStorageService,
        ILogger<VehiclesController> logger)
    {
        _vehicleRepository = vehicleRepository;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all vehicles with optional filtering
    /// </summary>
    /// <param name="status">Filter by vehicle status</param>
    /// <param name="make">Filter by vehicle make</param>
    /// <param name="model">Filter by vehicle model</param>
    /// <param name="year">Filter by vehicle year</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>List of vehicles matching the criteria</returns>
    /// <response code="200">Returns the list of vehicles</response>
    /// <response code="400">If the request parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VehicleDto>), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetVehicles(
        [FromQuery] VehicleStatus? status = null,
        [FromQuery] string? make = null,
        [FromQuery] string? model = null,
        [FromQuery] int? year = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Getting vehicles with filters - Status: {Status}, Make: {Make}, Model: {Model}, Year: {Year}, Page: {Page}, PageSize: {PageSize}",
                status, make, model, year, page, pageSize);

            IEnumerable<Vehicle> vehicles;

            if (status.HasValue)
            {
                vehicles = await _vehicleRepository.GetByStatusAsync(status.Value, page, pageSize);
            }
            else
            {
                vehicles = await _vehicleRepository.GetAllAsync();
            }

            // Apply additional filtering
            if (!string.IsNullOrEmpty(make))
            {
                vehicles = vehicles.Where(v => v.Make.Contains(make, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(model))
            {
                vehicles = vehicles.Where(v => v.Model.Contains(model, StringComparison.OrdinalIgnoreCase));
            }

            if (year.HasValue)
            {
                vehicles = vehicles.Where(v => v.Year == year.Value);
            }

            // Apply pagination if not already applied in repository
            if (!status.HasValue)
            {
                vehicles = vehicles.Skip((page - 1) * pageSize).Take(pageSize);
            }

            var vehicleDtos = vehicles.Select(MapToDto);
            return Ok(vehicleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicles");
            return StatusCode(500, new { error = "An error occurred while retrieving vehicles" });
        }
    }

    /// <summary>
    /// Get a specific vehicle by ID
    /// </summary>
    /// <param name="id">Vehicle ID</param>
    /// <returns>Vehicle details</returns>
    /// <response code="200">Returns the vehicle</response>
    /// <response code="404">If the vehicle is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VehicleDto), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<VehicleDto>> GetVehicle(int id)
    {
        try
        {
            _logger.LogInformation("Getting vehicle with ID: {VehicleId}", id);

            var vehicle = await _vehicleRepository.GetByIdAsync(id);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            var vehicleDto = MapToDto(vehicle);
            return Ok(vehicleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicle {VehicleId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the vehicle" });
        }
    }

    /// <summary>
    /// Create a new vehicle
    /// </summary>
    /// <param name="createVehicleDto">Vehicle data</param>
    /// <returns>Created vehicle</returns>
    /// <response code="201">Returns the newly created vehicle</response>
    /// <response code="400">If the vehicle data is invalid</response>
    /// <response code="409">If a vehicle with the same VIN already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), 201)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 409)]
    public async Task<ActionResult<VehicleDto>> CreateVehicle([FromBody] CreateVehicleDto createVehicleDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new vehicle with VIN: {Vin}", createVehicleDto.Vin);

            // Check if VIN already exists
            if (await _vehicleRepository.VinExistsAsync(createVehicleDto.Vin))
            {
                return Conflict(new { error = "A vehicle with this VIN already exists" });
            }

            var vehicle = MapFromCreateDto(createVehicleDto);
            var createdVehicle = await _vehicleRepository.AddAsync(vehicle);

            var createdVehicleDto = MapToDto(createdVehicle);
            return CreatedAtAction(nameof(GetVehicle), new { id = createdVehicleDto.Id }, createdVehicleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle");
            return StatusCode(500, new { error = "An error occurred while creating the vehicle" });
        }
    }

    /// <summary>
    /// Update an existing vehicle
    /// </summary>
    /// <param name="id">Vehicle ID</param>
    /// <param name="updateVehicleDto">Updated vehicle data</param>
    /// <returns>Updated vehicle</returns>
    /// <response code="200">Returns the updated vehicle</response>
    /// <response code="400">If the vehicle data is invalid</response>
    /// <response code="404">If the vehicle is not found</response>
    /// <response code="409">If a vehicle with the same VIN already exists</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(VehicleDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 409)]
    public async Task<ActionResult<VehicleDto>> UpdateVehicle(int id, [FromBody] UpdateVehicleDto updateVehicleDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating vehicle with ID: {VehicleId}", id);

            var existingVehicle = await _vehicleRepository.GetByIdAsync(id);
            if (existingVehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            // Check VIN uniqueness if VIN is being updated
            if (!string.IsNullOrEmpty(updateVehicleDto.Vin) &&
                updateVehicleDto.Vin != existingVehicle.Vin &&
                await _vehicleRepository.VinExistsAsync(updateVehicleDto.Vin, id))
            {
                return Conflict(new { error = "A vehicle with this VIN already exists" });
            }

            UpdateFromDto(existingVehicle, updateVehicleDto);
            var updatedVehicle = await _vehicleRepository.UpdateAsync(existingVehicle);

            var updatedVehicleDto = MapToDto(updatedVehicle);
            return Ok(updatedVehicleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle {VehicleId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the vehicle" });
        }
    }

    /// <summary>
    /// Delete a vehicle
    /// </summary>
    /// <param name="id">Vehicle ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Vehicle deleted successfully</response>
    /// <response code="404">If the vehicle is not found</response>
    /// <response code="409">If the vehicle has associated records that prevent deletion</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 409)]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        try
        {
            _logger.LogInformation("Deleting vehicle with ID: {VehicleId}", id);

            var vehicle = await _vehicleRepository.GetByIdAsync(id);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            // Check if vehicle has associated records that would prevent deletion
            // Note: This depends on your business rules - you might want to check for
            // associated fuel logs, maintenance records, insurance policies, etc.
            // For now, we'll allow deletion

            await _vehicleRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vehicle {VehicleId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the vehicle" });
        }
    }

    /// <summary>
    /// Get vehicle statistics (total count, by status, etc.)
    /// </summary>
    /// <returns>Vehicle statistics</returns>
    /// <response code="200">Returns vehicle statistics</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult> GetVehicleStatistics()
    {
        try
        {
            _logger.LogInformation("Getting vehicle statistics");

            var statistics = await _vehicleRepository.GetStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicle statistics");
            return StatusCode(500, new { error = "An error occurred while retrieving vehicle statistics" });
        }
    }

    /// <summary>
    /// Maps Vehicle entity to VehicleDto
    /// </summary>
    private static VehicleDto MapToDto(Vehicle vehicle)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            Name = vehicle.Name,
            Vin = vehicle.Vin,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Color = vehicle.Color,
            OdometerReading = vehicle.OdometerReading,
            Status = vehicle.Status,
            AcquisitionDate = vehicle.AcquisitionDate,
            Details = vehicle.Details,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt
        };
    }

    /// <summary>
    /// Maps CreateVehicleDto to Vehicle entity
    /// </summary>
    private static Vehicle MapFromCreateDto(CreateVehicleDto dto)
    {
        return new Vehicle
        {
            Name = dto.Name,
            Vin = dto.Vin,
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            LicensePlate = dto.LicensePlate,
            Color = dto.Color,
            OdometerReading = dto.OdometerReading,
            Status = dto.Status,
            AcquisitionDate = dto.AcquisitionDate,
            Details = dto.Details,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates Vehicle entity with values from UpdateVehicleDto
    /// </summary>
    private static void UpdateFromDto(Vehicle vehicle, UpdateVehicleDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Name))
            vehicle.Name = dto.Name;

        if (!string.IsNullOrEmpty(dto.Vin))
            vehicle.Vin = dto.Vin;

        if (!string.IsNullOrEmpty(dto.Make))
            vehicle.Make = dto.Make;

        if (!string.IsNullOrEmpty(dto.Model))
            vehicle.Model = dto.Model;

        if (dto.Year.HasValue)
            vehicle.Year = dto.Year.Value;

        if (dto.LicensePlate != null)
            vehicle.LicensePlate = dto.LicensePlate;

        if (dto.Color != null)
            vehicle.Color = dto.Color;

        if (dto.OdometerReading.HasValue)
            vehicle.OdometerReading = dto.OdometerReading.Value;

        if (dto.Status.HasValue)
            vehicle.Status = dto.Status.Value;

        if (dto.AcquisitionDate.HasValue)
            vehicle.AcquisitionDate = dto.AcquisitionDate;

        if (dto.Details != null)
            vehicle.Details = dto.Details;

        vehicle.UpdatedAt = DateTime.UtcNow;
    }
}
