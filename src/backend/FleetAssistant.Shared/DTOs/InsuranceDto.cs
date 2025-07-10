using FleetAssistant.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.Shared.DTOs;

/// <summary>
/// DTO for creating a new insurance policy
/// </summary>
public class CreateInsurancePolicyDto
{
    [Required]
    [StringLength(100)]
    public string PolicyNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ProviderName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ProviderContact { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [Range(0.01, 999999.99)]
    public decimal PremiumAmount { get; set; }

    public PaymentFrequency PaymentFrequency { get; set; } = PaymentFrequency.Monthly;

    [Range(0, 99999.99)]
    public decimal Deductible { get; set; }

    [Range(0, 9999999.99)]
    public decimal CoverageLimit { get; set; }

    public InsuranceCoverageType CoverageType { get; set; } = InsuranceCoverageType.Comprehensive;

    public string? CoverageDetails { get; set; }

    [StringLength(500)]
    public string? PolicyDocumentUrl { get; set; }

    public string? Notes { get; set; }

    public List<int>? VehicleIds { get; set; }
}

/// <summary>
/// DTO for updating an existing insurance policy
/// </summary>
public class UpdateInsurancePolicyDto
{
    [StringLength(100)]
    public string? PolicyNumber { get; set; }

    [StringLength(200)]
    public string? ProviderName { get; set; }

    [StringLength(500)]
    public string? ProviderContact { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? PremiumAmount { get; set; }

    public PaymentFrequency? PaymentFrequency { get; set; }

    [Range(0, 99999.99)]
    public decimal? Deductible { get; set; }

    [Range(0, 9999999.99)]
    public decimal? CoverageLimit { get; set; }

    public InsuranceCoverageType? CoverageType { get; set; }

    public string? CoverageDetails { get; set; }

    [StringLength(500)]
    public string? PolicyDocumentUrl { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// DTO for insurance policy response
/// </summary>
public class InsurancePolicyDto
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderContact { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PremiumAmount { get; set; }
    public PaymentFrequency PaymentFrequency { get; set; }
    public decimal Deductible { get; set; }
    public decimal CoverageLimit { get; set; }
    public InsuranceCoverageType CoverageType { get; set; }
    public string? CoverageDetails { get; set; }
    public string? PolicyDocumentUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<VehicleInsuranceDto> CoveredVehicles { get; set; } = new List<VehicleInsuranceDto>();
}

/// <summary>
/// DTO for vehicle insurance relationship
/// </summary>
public class VehicleInsuranceDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string? VehicleName { get; set; }
    public string? VehicleVin { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for adding/removing vehicles from insurance policy
/// </summary>
public class ManageVehicleInsuranceDto
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}
