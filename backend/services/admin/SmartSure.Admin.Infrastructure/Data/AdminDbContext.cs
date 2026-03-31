using Microsoft.EntityFrameworkCore;
using SmartSure.Admin.Domain.Entities;

namespace SmartSure.Admin.Infrastructure.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<AdminPolicy> AdminPolicies { get; set; }
    public DbSet<AdminClaim> AdminClaims { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<AdminPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PolicyNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.PolicyNumber).IsUnique();
        });

        modelBuilder.Entity<AdminClaim>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PolicyNumber).IsRequired().HasMaxLength(50);
        });
    }
}
