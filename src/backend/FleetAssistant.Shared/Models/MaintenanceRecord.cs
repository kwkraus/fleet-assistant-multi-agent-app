using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Represents a maintenance record for a vehicle
/// </summary>
public class MaintenanceRecord
{
    /// <summary>
    /// Unique identifier for the maintenance record
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the Vehicle
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Type of maintenance performed
    /// </summary>
    public MaintenanceType MaintenanceType { get; set; }

    /// <summary>
    /// Date when maintenance was performed
    /// </summary>
    [Required]
    public DateTime MaintenanceDate { get; set; }

    /// <summary>
    /// Odometer reading at time of maintenance
    /// </summary>
    [Range(0, int.MaxValue)]
    public int OdometerReading { get; set; }

    /// <summary>
    /// Description of work performed
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Cost of the maintenance work
    /// </summary>
    [Range(0, 99999.99)]
    [Column(TypeName = "decimal(8,2)")]
    public decimal Cost { get; set; }

    /// <summary>
    /// Service provider or mechanic name
    /// </summary>
    [StringLength(200)]
    public string? ServiceProvider { get; set; }

    /// <summary>
    /// Service provider contact information
    /// </summary>
    [StringLength(200)]
    public string? ServiceProviderContact { get; set; }

    /// <summary>
    /// Invoice or receipt number
    /// </summary>
    [StringLength(100)]
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Warranty information for the work performed
    /// </summary>
    [StringLength(500)]
    public string? WarrantyInfo { get; set; }

    /// <summary>
    /// Next scheduled maintenance date (for preventative maintenance)
    /// </summary>
    public DateTime? NextMaintenanceDate { get; set; }

    /// <summary>
    /// Next scheduled maintenance odometer reading
    /// </summary>
    public int? NextMaintenanceOdometer { get; set; }

    /// <summary>
    /// Additional notes about the maintenance
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

/// <summary>
/// Types of maintenance that can be performed on a vehicle
/// </summary>
public enum MaintenanceType
{
    OilChange,
    TireRotation,
    TireReplacement,
    BrakeService,
    TransmissionService,
    EngineRepair,
    AirFilterReplacement,
    BatteryReplacement,
    Inspection,
    Registration,
    Recall,
    PreventativeMaintenance,
    EmergencyRepair,
    Other
}
