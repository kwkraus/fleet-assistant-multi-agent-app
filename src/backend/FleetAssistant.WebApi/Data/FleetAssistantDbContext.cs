using FleetAssistant.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetAssistant.WebApi.Data;

/// <summary>
/// Entity Framework database context for Fleet Assistant application
/// </summary>
public class FleetAssistantDbContext : DbContext
{
    public FleetAssistantDbContext(DbContextOptions<FleetAssistantDbContext> options)
        : base(options)
    {
    }

    // DbSet properties for each entity
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<FuelLog> FuelLogs { get; set; } = null!;
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; } = null!;
    public DbSet<InsurancePolicy> InsurancePolicies { get; set; } = null!;
    public DbSet<VehicleInsurance> VehicleInsurances { get; set; } = null!;
    public DbSet<VehicleFinancial> VehicleFinancials { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Vehicle entity
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.Property(v => v.Id)
                .ValueGeneratedOnAdd();

            entity.Property(v => v.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(v => v.Vin)
                .IsRequired()
                .HasMaxLength(17);

            entity.Property(v => v.Make)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(v => v.Model)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(v => v.LicensePlate)
                .HasMaxLength(20);

            entity.Property(v => v.Color)
                .HasMaxLength(30);

            entity.Property(v => v.Status)
                .HasConversion<string>();

            entity.Property(v => v.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(v => v.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Create unique index on VIN
            entity.HasIndex(v => v.Vin)
                .IsUnique();

            // Configure relationships
            entity.HasMany(v => v.FuelLogs)
                .WithOne(fl => fl.Vehicle)
                .HasForeignKey(fl => fl.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.MaintenanceRecords)
                .WithOne(mr => mr.Vehicle)
                .HasForeignKey(mr => mr.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.FinancialRecords)
                .WithOne(vf => vf.Vehicle)
                .HasForeignKey(vf => vf.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.InsurancePolicies)
                .WithOne(vi => vi.Vehicle)
                .HasForeignKey(vi => vi.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FuelLog entity
        modelBuilder.Entity<FuelLog>(entity =>
        {
            entity.HasKey(fl => fl.Id);

            entity.Property(fl => fl.Id)
                .ValueGeneratedOnAdd();

            entity.Property(fl => fl.Gallons)
                .HasColumnType("decimal(6,2)")
                .IsRequired();

            entity.Property(fl => fl.PricePerGallon)
                .HasColumnType("decimal(5,2)")
                .IsRequired();

            entity.Property(fl => fl.TotalCost)
                .HasColumnType("decimal(7,2)")
                .IsRequired();

            entity.Property(fl => fl.MilesPerGallon)
                .HasColumnType("decimal(5,2)");

            entity.Property(fl => fl.Location)
                .HasMaxLength(200);

            entity.Property(fl => fl.FuelType)
                .HasMaxLength(50);

            entity.Property(fl => fl.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(fl => fl.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Create index on VehicleId and FuelDate for efficient queries
            entity.HasIndex(fl => new { fl.VehicleId, fl.FuelDate });
        });

        // Configure MaintenanceRecord entity
        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(mr => mr.Id);

            entity.Property(mr => mr.Id)
                .ValueGeneratedOnAdd();

            entity.Property(mr => mr.MaintenanceType)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(mr => mr.Description)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(mr => mr.Cost)
                .HasColumnType("decimal(8,2)");

            entity.Property(mr => mr.ServiceProvider)
                .HasMaxLength(200);

            entity.Property(mr => mr.ServiceProviderContact)
                .HasMaxLength(200);

            entity.Property(mr => mr.InvoiceNumber)
                .HasMaxLength(100);

            entity.Property(mr => mr.WarrantyInfo)
                .HasMaxLength(500);

            entity.Property(mr => mr.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(mr => mr.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Create index on VehicleId and MaintenanceDate for efficient queries
            entity.HasIndex(mr => new { mr.VehicleId, mr.MaintenanceDate });

            // Create index on NextMaintenanceDate for upcoming maintenance queries
            entity.HasIndex(mr => mr.NextMaintenanceDate);
        });

        // Configure InsurancePolicy entity
        modelBuilder.Entity<InsurancePolicy>(entity =>
        {
            entity.HasKey(ip => ip.Id);

            entity.Property(ip => ip.Id)
                .ValueGeneratedOnAdd();

            entity.Property(ip => ip.PolicyNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(ip => ip.ProviderName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(ip => ip.ProviderContact)
                .HasMaxLength(500);

            entity.Property(ip => ip.PremiumAmount)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(ip => ip.PaymentFrequency)
                .HasConversion<string>();

            entity.Property(ip => ip.Deductible)
                .HasColumnType("decimal(8,2)");

            entity.Property(ip => ip.CoverageLimit)
                .HasColumnType("decimal(10,2)");

            entity.Property(ip => ip.CoverageType)
                .HasConversion<string>();

            entity.Property(ip => ip.PolicyDocumentUrl)
                .HasMaxLength(500);

            entity.Property(ip => ip.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(ip => ip.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Create unique index on PolicyNumber
            entity.HasIndex(ip => ip.PolicyNumber)
                .IsUnique();

            // Create index on EndDate for expiring policies queries
            entity.HasIndex(ip => ip.EndDate);

            // Configure relationships
            entity.HasMany(ip => ip.VehicleInsurances)
                .WithOne(vi => vi.InsurancePolicy)
                .HasForeignKey(vi => vi.InsurancePolicyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure VehicleInsurance entity (junction table)
        modelBuilder.Entity<VehicleInsurance>(entity =>
        {
            entity.HasKey(vi => vi.Id);

            // Create composite index on VehicleId and InsurancePolicyId
            entity.HasIndex(vi => new { vi.VehicleId, vi.InsurancePolicyId });

            // Create index on active status for filtering
            entity.HasIndex(vi => vi.IsActive);
        });

        // Configure VehicleFinancial entity
        modelBuilder.Entity<VehicleFinancial>(entity =>
        {
            entity.HasKey(vf => vf.Id);

            entity.Property(vf => vf.Id)
                .ValueGeneratedOnAdd();

            entity.Property(vf => vf.FinancialType)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(vf => vf.Amount)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(vf => vf.PaymentFrequency)
                .HasConversion<string>();

            entity.Property(vf => vf.ProviderName)
                .HasMaxLength(200);

            entity.Property(vf => vf.AccountNumber)
                .HasMaxLength(100);

            entity.Property(vf => vf.InterestRate)
                .HasColumnType("decimal(5,2)");

            entity.Property(vf => vf.RemainingBalance)
                .HasColumnType("decimal(10,2)");

            entity.Property(vf => vf.PurchasePrice)
                .HasColumnType("decimal(10,2)");

            entity.Property(vf => vf.CurrentValue)
                .HasColumnType("decimal(10,2)");

            entity.Property(vf => vf.DepreciationMethod)
                .HasConversion<string>();

            entity.Property(vf => vf.DepreciationRate)
                .HasColumnType("decimal(5,2)");

            entity.Property(vf => vf.DocumentUrl)
                .HasMaxLength(500);

            entity.Property(vf => vf.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(vf => vf.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Create index on VehicleId and FinancialType for efficient queries
            entity.HasIndex(vf => new { vf.VehicleId, vf.FinancialType });

            // Create index on NextPaymentDate for upcoming payments queries
            entity.HasIndex(vf => vf.NextPaymentDate);
        });

        // Configure automatic timestamp updates
        ConfigureTimestamps(modelBuilder);
    }

    /// <summary>
    /// Configure automatic timestamp updates for entities
    /// </summary>
    private static void ConfigureTimestamps(ModelBuilder modelBuilder)
    {
        // Note: Automatic timestamp updates are handled in SaveChanges methods
        // This method is kept for future extensions if needed
    }

    /// <summary>
    /// Override SaveChanges to automatically update timestamps
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically update timestamps
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Update timestamps for modified entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is not null &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var entityType = entity.GetType();

            if (entry.State == EntityState.Added)
            {
                var createdAtProperty = entityType.GetProperty("CreatedAt");
                if (createdAtProperty != null && createdAtProperty.CanWrite)
                {
                    createdAtProperty.SetValue(entity, DateTime.UtcNow);
                }
            }

            var updatedAtProperty = entityType.GetProperty("UpdatedAt");
            if (updatedAtProperty != null && updatedAtProperty.CanWrite)
            {
                updatedAtProperty.SetValue(entity, DateTime.UtcNow);
            }
        }
    }
}
