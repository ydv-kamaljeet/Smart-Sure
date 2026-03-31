using Microsoft.EntityFrameworkCore;
using SmartSure.Policy.Domain.Entities;
using MassTransit;

namespace SmartSure.Policy.Infrastructure.Data;

public class PolicyDbContext : DbContext
{
    public PolicyDbContext(DbContextOptions<PolicyDbContext> options) : base(options) { }

    public DbSet<InsuranceType> InsuranceTypes { get; set; } = null!;
    public DbSet<InsuranceSubType> InsuranceSubTypes { get; set; } = null!;
    public DbSet<Domain.Entities.Policy> Policies { get; set; } = null!;
    public DbSet<HomeDetails> HomeDetails { get; set; } = null!;
    public DbSet<VehicleDetails> VehicleDetails { get; set; } = null!;
    public DbSet<PaymentRecord> PaymentRecords { get; set; } = null!;
    public DbSet<PolicyDocument> PolicyDocuments { get; set; } = null!;
    public DbSet<PolicyHolder> PolicyHolders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<PolicyHolder>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.FullName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(250).IsRequired();
        });

        modelBuilder.Entity<InsuranceType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<InsuranceSubType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.BasePremium).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.InsuranceType)
                  .WithMany(t => t.SubTypes)
                  .HasForeignKey(e => e.InsuranceTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Domain.Entities.Policy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId); // Important for fast lookups per user
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PremiumAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.InsuredDeclaredValue).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.InsuranceSubType)
                  .WithMany()
                  .HasForeignKey(e => e.InsuranceSubTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HomeDetails>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyValue).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Policy)
                  .WithOne(p => p.HomeDetails)
                  .HasForeignKey<HomeDetails>(e => e.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VehicleDetails>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ListedPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Policy)
                  .WithOne(p => p.VehicleDetails)
                  .HasForeignKey<VehicleDetails>(e => e.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Policy)
                  .WithMany(p => p.Payments)
                  .HasForeignKey(e => e.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Policy)
                  .WithMany()
                  .HasForeignKey(e => e.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
