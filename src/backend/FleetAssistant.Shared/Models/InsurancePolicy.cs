using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Represents insurance information for vehicles
/// </summary>
public class InsurancePolicy
{
    /// <summary>
    /// Unique identifier for the insurance policy
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Policy number
    /// </summary>
    [Required]
    [StringLength(100)]
    public string PolicyNumber { get; set; } = string.Empty;

    /// <summary>
    /// Insurance provider name
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Insurance provider contact information
    /// </summary>
    [StringLength(500)]
    public string? ProviderContact { get; set; }

    /// <summary>
    /// Policy start date
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Policy end date
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Premium amount per payment period
    /// </summary>
    [Required]
    [Range(0.01, 999999.99)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PremiumAmount { get; set; }

    /// <summary>
    /// Payment frequency for premiums
    /// </summary>
    public PaymentFrequency PaymentFrequency { get; set; } = PaymentFrequency.Monthly;

    /// <summary>
    /// Deductible amount
    /// </summary>
    [Range(0, 99999.99)]
    [Column(TypeName = "decimal(8,2)")]
    public decimal Deductible { get; set; }

    /// <summary>
    /// Coverage limit amount
    /// </summary>
    [Range(0, 9999999.99)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CoverageLimit { get; set; }

    /// <summary>
    /// Type of insurance coverage
    /// </summary>
    public InsuranceCoverageType CoverageType { get; set; } = InsuranceCoverageType.Comprehensive;

    /// <summary>
    /// Additional coverage details
    /// </summary>
    public string? CoverageDetails { get; set; }

    /// <summary>
    /// Policy document file path or URL
    /// </summary>
    [StringLength(500)]
    public string? PolicyDocumentUrl { get; set; }

    /// <summary>
    /// Additional notes about the policy
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
    public ICollection<VehicleInsurance> VehicleInsurances { get; set; } = new List<VehicleInsurance>();
}

/// <summary>
/// Junction table for many-to-many relationship between vehicles and insurance policies
/// </summary>
public class VehicleInsurance
{
    /// <summary>
    /// Unique identifier for the vehicle insurance relationship
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the Vehicle
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Foreign key to the Insurance Policy
    /// </summary>
    public int InsurancePolicyId { get; set; }

    /// <summary>
    /// Date when this vehicle was added to the policy
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Date when this vehicle was removed from the policy (if applicable)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether this vehicle is currently covered by this policy
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Vehicle Vehicle { get; set; } = null!;
    public InsurancePolicy InsurancePolicy { get; set; } = null!;
}

/// <summary>
/// Types of insurance coverage
/// </summary>
public enum InsuranceCoverageType
{
    Liability,
    Collision,
    Comprehensive,
    FullCoverage,
    CommercialAuto
}

/// <summary>
/// Payment frequency options
/// </summary>
public enum PaymentFrequency
{
    Monthly,
    Quarterly,
    SemiAnnually,
    Annually
}
