using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Represents a fleet vehicle with essential information for management
/// </summary>
public class Vehicle
{
    /// <summary>
    /// Unique identifier for the vehicle
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Friendly name or identifier for the vehicle
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle Identification Number
    /// </summary>
    [Required]
    [StringLength(17, MinimumLength = 17)]
    public string Vin { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle make (e.g., Ford, Toyota)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Make { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle model (e.g., F-150, Camry)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Model year of the vehicle
    /// </summary>
    [Range(1900, 2100)]
    public int Year { get; set; }

    /// <summary>
    /// License plate number
    /// </summary>
    [StringLength(20)]
    public string? LicensePlate { get; set; }

    /// <summary>
    /// Vehicle color
    /// </summary>
    [StringLength(30)]
    public string? Color { get; set; }

    /// <summary>
    /// Current odometer reading in miles
    /// </summary>
    [Range(0, int.MaxValue)]
    public int OdometerReading { get; set; }

    /// <summary>
    /// Current status of the vehicle
    /// </summary>
    public VehicleStatus Status { get; set; } = VehicleStatus.Active;

    /// <summary>
    /// Date the vehicle was acquired
    /// </summary>
    public DateTime? AcquisitionDate { get; set; }

    /// <summary>
    /// Additional details or notes about the vehicle
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Date when the record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
    public ICollection<VehicleInsurance> InsurancePolicies { get; set; } = new List<VehicleInsurance>();
    public ICollection<VehicleFinancial> FinancialRecords { get; set; } = new List<VehicleFinancial>();
}

/// <summary>
/// Represents the current status of a vehicle
/// </summary>
public enum VehicleStatus
{
    Active,
    InMaintenance,
    OutOfService,
    Sold,
    Retired
}
