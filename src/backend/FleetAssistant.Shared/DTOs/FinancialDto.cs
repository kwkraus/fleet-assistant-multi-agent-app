using FleetAssistant.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.Shared.DTOs;

/// <summary>
/// DTO for creating a new vehicle financial record
/// </summary>
public class CreateVehicleFinancialDto
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public FinancialType FinancialType { get; set; }

    [Required]
    [Range(0.01, 9999999.99)]
    public decimal Amount { get; set; }

    public PaymentFrequency? PaymentFrequency { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? NextPaymentDate { get; set; }

    [StringLength(200)]
    public string? ProviderName { get; set; }

    [StringLength(100)]
    public string? AccountNumber { get; set; }

    [Range(0, 100)]
    public decimal? InterestRate { get; set; }

    [Range(0, 9999999.99)]
    public decimal? RemainingBalance { get; set; }

    [Range(0, 9999999.99)]
    public decimal? PurchasePrice { get; set; }

    [Range(0, 9999999.99)]
    public decimal? CurrentValue { get; set; }

    public DepreciationMethod? DepreciationMethod { get; set; }

    [Range(0, 100)]
    public decimal? DepreciationRate { get; set; }

    [StringLength(500)]
    public string? DocumentUrl { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating an existing vehicle financial record
/// </summary>
public class UpdateVehicleFinancialDto
{
    public FinancialType? FinancialType { get; set; }

    [Range(0.01, 9999999.99)]
    public decimal? Amount { get; set; }

    public PaymentFrequency? PaymentFrequency { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? NextPaymentDate { get; set; }

    [StringLength(200)]
    public string? ProviderName { get; set; }

    [StringLength(100)]
    public string? AccountNumber { get; set; }

    [Range(0, 100)]
    public decimal? InterestRate { get; set; }

    [Range(0, 9999999.99)]
    public decimal? RemainingBalance { get; set; }

    [Range(0, 9999999.99)]
    public decimal? PurchasePrice { get; set; }

    [Range(0, 9999999.99)]
    public decimal? CurrentValue { get; set; }

    public DepreciationMethod? DepreciationMethod { get; set; }

    [Range(0, 100)]
    public decimal? DepreciationRate { get; set; }

    [StringLength(500)]
    public string? DocumentUrl { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// DTO for vehicle financial record response
/// </summary>
public class VehicleFinancialDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public FinancialType FinancialType { get; set; }
    public decimal Amount { get; set; }
    public PaymentFrequency? PaymentFrequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public string? ProviderName { get; set; }
    public string? AccountNumber { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? RemainingBalance { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? CurrentValue { get; set; }
    public DepreciationMethod? DepreciationMethod { get; set; }
    public decimal? DepreciationRate { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? VehicleName { get; set; }
}
