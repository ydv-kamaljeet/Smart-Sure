using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartSure.Claims.Domain.Entities;

namespace SmartSure.Claims.Infrastructure.Data;

public class ClaimsDbContext : DbContext
{
    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : base(options)
    {
    }

    public DbSet<Claim> Claims { get; set; }
    public DbSet<ClaimDocument> ClaimDocuments { get; set; }
    public DbSet<ClaimHistory> ClaimHistories { get; set; }
    public DbSet<ValidPolicy> ValidPolicies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<ValidPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PolicyId).IsUnique();
            entity.Property(e => e.InsuredDeclaredValue).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasMany(c => c.Documents)
                  .WithOne(cd => cd.Claim)
                  .HasForeignKey(cd => cd.ClaimId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.History)
                  .WithOne(ch => ch.Claim)
                  .HasForeignKey(ch => ch.ClaimId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // Indexes for fast lookups
            entity.HasIndex(c => c.UserId);
            entity.HasIndex(c => c.PolicyId);
        });

        modelBuilder.Entity<ClaimDocument>(entity =>
        {
            entity.HasKey(cd => cd.Id);
        });

        modelBuilder.Entity<ClaimHistory>(entity =>
        {
            entity.HasKey(ch => ch.Id);
        });
    }
}
