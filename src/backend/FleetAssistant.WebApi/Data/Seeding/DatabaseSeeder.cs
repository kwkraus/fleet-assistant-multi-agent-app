using FleetAssistant.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetAssistant.WebApi.Data.Seeding;

/// <summary>
/// Seeds the in-memory database with representative sample data for development and testing.
/// Vehicles are created first so dependent entities can reference stable IDs.
/// </summary>
public class DatabaseSeeder(FleetAssistantDbContext context, ILogger<DatabaseSeeder> logger) : IDatabaseSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotency check – if any vehicles already exist we assume seeding ran.
        if (await context.Vehicles.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded – skipping.");
            return;
        }

        logger.LogInformation("Starting database seeding...");

        try
        {
            // 1. Vehicles (explicit IDs for deterministic references)
            var vehicles = new List<Vehicle>
            {
                new() { Id = 1, Name = "Unit 1 Truck", Vin = "1FTFW1E50PFA12345", Make = "Ford", Model = "F-150", Year = 2023, LicensePlate = "TRK001", Color = "Blue", OdometerReading = 18250, Status = VehicleStatus.Active, AcquisitionDate = DateTime.UtcNow.AddMonths(-18), Details = "Primary delivery truck" },
                new() { Id = 2, Name = "Unit 2 Van", Vin = "2GCEK19T1Y1234567", Make = "Chevrolet", Model = "Express", Year = 2022, LicensePlate = "VAN002", Color = "White", OdometerReading = 42310, Status = VehicleStatus.InMaintenance, AcquisitionDate = DateTime.UtcNow.AddMonths(-28), Details = "Cargo van awaiting brake service" },
                new() { Id = 3, Name = "Unit 3 Hybrid", Vin = "JTDKB20U693876543", Make = "Toyota", Model = "Prius", Year = 2021, LicensePlate = "HYB003", Color = "Silver", OdometerReading = 60500, Status = VehicleStatus.Active, AcquisitionDate = DateTime.UtcNow.AddYears(-3), Details = "Fuel efficient route vehicle" },
                new() { Id = 4, Name = "Unit 4 SUV", Vin = "1FM5K8GC6NGA98765", Make = "Ford", Model = "Explorer", Year = 2024, LicensePlate = "SUV004", Color = "Black", OdometerReading = 7800, Status = VehicleStatus.Active, AcquisitionDate = DateTime.UtcNow.AddMonths(-6), Details = "Supervisor vehicle" },
                new() { Id = 5, Name = "Unit 5 Retired", Vin = "1GCHK23D57F198765", Make = "GMC", Model = "Sierra", Year = 2019, LicensePlate = "OLD005", Color = "Red", OdometerReading = 152300, Status = VehicleStatus.Retired, AcquisitionDate = DateTime.UtcNow.AddYears(-5), Details = "Awaiting auction" }
            };
            await context.Vehicles.AddRangeAsync(vehicles, cancellationToken);

            // 2. Fuel Logs (subset of vehicles 1-3)
            var now = DateTime.UtcNow;
            var fuelLogs = new List<FuelLog>
            {
                Fuel(1, now.AddDays(-14), 18100, 18.42m, 3.59m, "Shell – Downtown", "Regular", "Route A refuel"),
                Fuel(1, now.AddDays(-7), 18210, 17.05m, 3.55m, "Chevron – I90", "Regular", null),
                Fuel(2, now.AddDays(-10), 42010, 22.10m, 3.79m, "Costco Fuel", "Regular", "Pre-maintenance"),
                Fuel(2, now.AddDays(-3), 42300, 21.75m, 3.82m, "BP – Industrial Park", "Regular", null),
                Fuel(3, now.AddDays(-8), 60250, 9.80m, 3.25m, "EV Hybrid Station", "Regular", "Mixed driving"),
                Fuel(3, now.AddDays(-2), 60490, 10.15m, 3.28m, "EV Hybrid Station", "Regular", null)
            };
            await context.FuelLogs.AddRangeAsync(fuelLogs, cancellationToken);

            // 3. Maintenance Records
            var maintenanceRecords = new List<MaintenanceRecord>
            {
                new() { VehicleId = 1, MaintenanceType = MaintenanceType.OilChange, MaintenanceDate = now.AddDays(-30), OdometerReading = 17500, Description = "Full synthetic oil & filter", Cost = 89.99m, ServiceProvider = "QuickLube", InvoiceNumber = "INV-OC-001", NextMaintenanceDate = now.AddDays(60), NextMaintenanceOdometer = 23500 },
                new() { VehicleId = 2, MaintenanceType = MaintenanceType.BrakeService, MaintenanceDate = now.AddDays(-5), OdometerReading = 42250, Description = "Front brake pads & rotors", Cost = 540.00m, ServiceProvider = "BrakePro", InvoiceNumber = "INV-BR-014" },
                new() { VehicleId = 3, MaintenanceType = MaintenanceType.TireRotation, MaintenanceDate = now.AddDays(-40), OdometerReading = 59000, Description = "Tire rotation & pressure check", Cost = 45.00m, ServiceProvider = "TireCenter", InvoiceNumber = "INV-TR-210", NextMaintenanceDate = now.AddDays(50), NextMaintenanceOdometer = 65000 }
            };
            await context.MaintenanceRecords.AddRangeAsync(maintenanceRecords, cancellationToken);

            // 4. Insurance Policies
            var policyStart1 = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var policyStart2 = policyStart1.AddMonths(6);
            var insurancePolicies = new List<InsurancePolicy>
            {
                new() { Id = 1001, PolicyNumber = "POL-TRUCK-FLEET-001", ProviderName = "Acme Insurance Co.", StartDate = policyStart1, EndDate = policyStart1.AddYears(1), PremiumAmount = 1200.00m, PaymentFrequency = PaymentFrequency.Monthly, Deductible = 500m, CoverageLimit = 1000000m, CoverageType = InsuranceCoverageType.CommercialAuto, CoverageDetails = "Fleet liability & collision" },
                new() { Id = 1002, PolicyNumber = "POL-EXEC-002", ProviderName = "Global Mutual", StartDate = policyStart2, EndDate = policyStart2.AddYears(1), PremiumAmount = 1800.00m, PaymentFrequency = PaymentFrequency.Quarterly, Deductible = 750m, CoverageLimit = 1500000m, CoverageType = InsuranceCoverageType.FullCoverage, CoverageDetails = "Executive / specialty vehicles" }
            };
            await context.InsurancePolicies.AddRangeAsync(insurancePolicies, cancellationToken);

            // 5. Vehicle-Insurance (junction)
            var vehicleInsurances = new List<VehicleInsurance>
            {
                new() { VehicleId = 1, InsurancePolicyId = 1001, StartDate = policyStart1, IsActive = true },
                new() { VehicleId = 2, InsurancePolicyId = 1001, StartDate = policyStart1, IsActive = true },
                new() { VehicleId = 3, InsurancePolicyId = 1001, StartDate = policyStart1.AddMonths(1), IsActive = true },
                new() { VehicleId = 4, InsurancePolicyId = 1002, StartDate = policyStart2, IsActive = true },
                new() { VehicleId = 5, InsurancePolicyId = 1001, StartDate = policyStart1, EndDate = policyStart1.AddMonths(3), IsActive = false }
            };
            await context.VehicleInsurances.AddRangeAsync(vehicleInsurances, cancellationToken);

            // 6. Vehicle Financials
            var financials = new List<VehicleFinancial>
            {
                new() { VehicleId = 1, FinancialType = FinancialType.Purchase, Amount = 52000m, StartDate = vehicles[0].AcquisitionDate!.Value, ProviderName = "Ford Dealer", PurchasePrice = 52000m, CurrentValue = 47000m, DepreciationMethod = DepreciationMethod.StraightLine, DepreciationRate = 15m },
                new() { VehicleId = 1, FinancialType = FinancialType.Registration, Amount = 450m, StartDate = policyStart1, Notes = "Annual registration" },
                new() { VehicleId = 2, FinancialType = FinancialType.Loan, Amount = 750.00m, PaymentFrequency = PaymentFrequency.Monthly, StartDate = vehicles[1].AcquisitionDate!.Value, ProviderName = "BankCorp", InterestRate = 4.25m, RemainingBalance = 18800m, PurchasePrice = 43000m, CurrentValue = 36000m },
                new() { VehicleId = 3, FinancialType = FinancialType.Lease, Amount = 420.00m, PaymentFrequency = PaymentFrequency.Monthly, StartDate = vehicles[2].AcquisitionDate!.Value, ProviderName = "AutoLease LLC", InterestRate = 2.95m, RemainingBalance = 9800m },
                new() { VehicleId = 4, FinancialType = FinancialType.Purchase, Amount = 61000m, StartDate = vehicles[3].AcquisitionDate!.Value, ProviderName = "Ford Dealer", PurchasePrice = 61000m, CurrentValue = 60000m, DepreciationMethod = DepreciationMethod.MACRS, DepreciationRate = 20m },
                new() { VehicleId = 5, FinancialType = FinancialType.Depreciation, Amount = 0m, StartDate = vehicles[4].AcquisitionDate!.Value, Notes = "Fully depreciated" }
            };
            await context.VehicleFinancials.AddRangeAsync(financials, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database seeding failed: {Message}", ex.Message);
            throw; // rethrow to surface during startup.
        }
    }

    private static FuelLog Fuel(int vehicleId, DateTime dt, int odometer, decimal gallons, decimal pricePerGallon, string location, string fuelType, string? notes) =>
        new()
        {
            VehicleId = vehicleId,
            FuelDate = dt,
            OdometerReading = odometer,
            Gallons = gallons,
            PricePerGallon = pricePerGallon,
            TotalCost = Math.Round(gallons * pricePerGallon, 2),
            Location = location,
            FuelType = fuelType,
            Notes = notes,
            MilesPerGallon = null // could be computed later
        };
}
