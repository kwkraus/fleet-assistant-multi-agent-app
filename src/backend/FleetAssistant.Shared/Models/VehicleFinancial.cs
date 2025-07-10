using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Represents financial information for a vehicle (lease, finance, registration, depreciation)
/// </summary>
public class VehicleFinancial
{
    /// <summary>
    /// Unique identifier for the financial record
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the Vehicle
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Type of financial record
    /// </summary>
    public FinancialType FinancialType { get; set; }

    /// <summary>
    /// Amount of the financial obligation
    /// </summary>
    [Required]
    [Range(0.01, 9999999.99)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment frequency (for recurring payments like lease/finance)
    /// </summary>
    public PaymentFrequency? PaymentFrequency { get; set; }

    /// <summary>
    /// Start date of the financial obligation
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the financial obligation (if applicable)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Due date for the next payment
    /// </summary>
    public DateTime? NextPaymentDate { get; set; }

    /// <summary>
    /// Financial institution or provider name
    /// </summary>
    [StringLength(200)]
    public string? ProviderName { get; set; }

    /// <summary>
    /// Account or reference number
    /// </summary>
    [StringLength(100)]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Interest rate (for loans/leases)
    /// </summary>
    [Range(0, 100)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal? InterestRate { get; set; }

    /// <summary>
    /// Remaining balance (for loans)
    /// </summary>
    [Range(0, 9999999.99)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal? RemainingBalance { get; set; }

    /// <summary>
    /// Purchase price or initial value of the vehicle
    /// </summary>
    [Range(0, 9999999.99)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// Current estimated value of the vehicle
    /// </summary>
    [Range(0, 9999999.99)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal? CurrentValue { get; set; }

    /// <summary>
    /// Depreciation method used
    /// </summary>
    public DepreciationMethod? DepreciationMethod { get; set; }

    /// <summary>
    /// Annual depreciation rate percentage
    /// </summary>
    [Range(0, 100)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DepreciationRate { get; set; }

    /// <summary>
    /// Document file path or URL for financial documents
    /// </summary>
    [StringLength(500)]
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Additional notes about the financial record
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
/// Types of financial records
/// </summary>
public enum FinancialType
{
    Purchase,
    Lease,
    Loan,
    Registration,
    Licensing,
    Taxes,
    Depreciation,
    Insurance,
    Other
}

/// <summary>
/// Depreciation calculation methods
/// </summary>
public enum DepreciationMethod
{
    StraightLine,
    DoubleDecliningBalance,
    SumOfYearsDigits,
    UnitsOfProduction,
    MACRS
}
