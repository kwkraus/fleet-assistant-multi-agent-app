using FleetAssistant.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.Shared.DTOs;

/// <summary>
/// DTO for creating a new vehicle
/// </summary>
public class CreateVehicleDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(17, MinimumLength = 17)]
    public string Vin { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [Range(1900, 2100)]
    public int Year { get; set; }

    [StringLength(20)]
    public string? LicensePlate { get; set; }

    [StringLength(30)]
    public string? Color { get; set; }

    [Range(0, int.MaxValue)]
    public int OdometerReading { get; set; }

    public VehicleStatus Status { get; set; } = VehicleStatus.Active;

    public DateTime? AcquisitionDate { get; set; }

    public string? Details { get; set; }
}

/// <summary>
/// DTO for updating an existing vehicle
/// </summary>
public class UpdateVehicleDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(17, MinimumLength = 17)]
    public string? Vin { get; set; }

    [StringLength(50)]
    public string? Make { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    [Range(1900, 2100)]
    public int? Year { get; set; }

    [StringLength(20)]
    public string? LicensePlate { get; set; }

    [StringLength(30)]
    public string? Color { get; set; }

    [Range(0, int.MaxValue)]
    public int? OdometerReading { get; set; }

    public VehicleStatus? Status { get; set; }

    public DateTime? AcquisitionDate { get; set; }

    public string? Details { get; set; }
}

/// <summary>
/// DTO for vehicle response
/// </summary>
public class VehicleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? LicensePlate { get; set; }
    public string? Color { get; set; }
    public int OdometerReading { get; set; }
    public VehicleStatus Status { get; set; }
    public DateTime? AcquisitionDate { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
