using FleetAssistant.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.Shared.DTOs;

/// <summary>
/// DTO for creating a new maintenance record
/// </summary>
public class CreateMaintenanceRecordDto
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public MaintenanceType MaintenanceType { get; set; }

    [Required]
    public DateTime MaintenanceDate { get; set; }

    [Range(0, int.MaxValue)]
    public int OdometerReading { get; set; }

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 99999.99)]
    public decimal Cost { get; set; }

    [StringLength(200)]
    public string? ServiceProvider { get; set; }

    [StringLength(200)]
    public string? ServiceProviderContact { get; set; }

    [StringLength(100)]
    public string? InvoiceNumber { get; set; }

    [StringLength(500)]
    public string? WarrantyInfo { get; set; }

    public DateTime? NextMaintenanceDate { get; set; }

    public int? NextMaintenanceOdometer { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating an existing maintenance record
/// </summary>
public class UpdateMaintenanceRecordDto
{
    public MaintenanceType? MaintenanceType { get; set; }

    public DateTime? MaintenanceDate { get; set; }

    [Range(0, int.MaxValue)]
    public int? OdometerReading { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, 99999.99)]
    public decimal? Cost { get; set; }

    [StringLength(200)]
    public string? ServiceProvider { get; set; }

    [StringLength(200)]
    public string? ServiceProviderContact { get; set; }

    [StringLength(100)]
    public string? InvoiceNumber { get; set; }

    [StringLength(500)]
    public string? WarrantyInfo { get; set; }

    public DateTime? NextMaintenanceDate { get; set; }

    public int? NextMaintenanceOdometer { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// DTO for maintenance record response
/// </summary>
public class MaintenanceRecordDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public DateTime MaintenanceDate { get; set; }
    public int OdometerReading { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string? ServiceProvider { get; set; }
    public string? ServiceProviderContact { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? WarrantyInfo { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public int? NextMaintenanceOdometer { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? VehicleName { get; set; }
}
