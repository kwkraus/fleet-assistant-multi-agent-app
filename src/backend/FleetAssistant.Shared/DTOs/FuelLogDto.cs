using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.Shared.DTOs;

/// <summary>
/// DTO for creating a new fuel log entry
/// </summary>
public class CreateFuelLogDto
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public DateTime FuelDate { get; set; }

    [Range(0, int.MaxValue)]
    public int OdometerReading { get; set; }

    [Required]
    [Range(0.01, 999.99)]
    public decimal Gallons { get; set; }

    [Required]
    [Range(0.01, 99.99)]
    public decimal PricePerGallon { get; set; }

    [Required]
    [Range(0.01, 9999.99)]
    public decimal TotalCost { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(50)]
    public string? FuelType { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating an existing fuel log entry
/// </summary>
public class UpdateFuelLogDto
{
    public DateTime? FuelDate { get; set; }

    [Range(0, int.MaxValue)]
    public int? OdometerReading { get; set; }

    [Range(0.01, 999.99)]
    public decimal? Gallons { get; set; }

    [Range(0.01, 99.99)]
    public decimal? PricePerGallon { get; set; }

    [Range(0.01, 9999.99)]
    public decimal? TotalCost { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(50)]
    public string? FuelType { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// DTO for fuel log response
/// </summary>
public class FuelLogDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public DateTime FuelDate { get; set; }
    public int OdometerReading { get; set; }
    public decimal Gallons { get; set; }
    public decimal PricePerGallon { get; set; }
    public decimal TotalCost { get; set; }
    public string? Location { get; set; }
    public string? FuelType { get; set; }
    public decimal? MilesPerGallon { get; set; }
    public int? MilesDriven { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? VehicleName { get; set; }
}
