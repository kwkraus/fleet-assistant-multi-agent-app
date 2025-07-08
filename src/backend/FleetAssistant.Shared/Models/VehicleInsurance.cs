using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Junction entity representing the relationship between vehicles and insurance policies
/// </summary>
public class VehicleInsurance
{
    /// <summary>
    /// Unique identifier for the vehicle-insurance relationship
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the Vehicle
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Foreign key to the InsurancePolicy
    /// </summary>
    public int InsurancePolicyId { get; set; }

    /// <summary>
    /// Whether this insurance policy is currently active for the vehicle
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date when this insurance coverage started for the vehicle
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Date when this insurance coverage ended for the vehicle (if applicable)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Additional notes about this vehicle-insurance relationship
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

    // Navigation properties
    public Vehicle Vehicle { get; set; } = null!;
    public InsurancePolicy InsurancePolicy { get; set; } = null!;
}
