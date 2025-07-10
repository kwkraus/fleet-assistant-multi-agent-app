using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Represents a fuel log entry for a vehicle
/// </summary>
public class FuelLog
{
    /// <summary>
    /// Unique identifier for the fuel log entry
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the Vehicle
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Date and time when fuel was purchased
    /// </summary>
    [Required]
    public DateTime FuelDate { get; set; }

    /// <summary>
    /// Odometer reading at time of fuel purchase
    /// </summary>
    [Range(0, int.MaxValue)]
    public int OdometerReading { get; set; }

    /// <summary>
    /// Amount of fuel purchased in gallons
    /// </summary>
    [Required]
    [Range(0.01, 999.99)]
    [Column(TypeName = "decimal(6,2)")]
    public decimal Gallons { get; set; }

    /// <summary>
    /// Price per gallon
    /// </summary>
    [Required]
    [Range(0.01, 99.99)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal PricePerGallon { get; set; }

    /// <summary>
    /// Total cost of fuel purchase
    /// </summary>
    [Required]
    [Range(0.01, 9999.99)]
    [Column(TypeName = "decimal(7,2)")]
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Location where fuel was purchased
    /// </summary>
    [StringLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// Type of fuel (Regular, Premium, Diesel, etc.)
    /// </summary>
    [StringLength(50)]
    public string? FuelType { get; set; }

    /// <summary>
    /// Calculated miles per gallon (if available)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MilesPerGallon { get; set; }

    /// <summary>
    /// Miles driven since last fuel entry
    /// </summary>
    public int? MilesDriven { get; set; }

    /// <summary>
    /// Additional notes about the fuel purchase
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Date when the record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Vehicle Vehicle { get; set; } = null!;
}
