using Microsoft.EntityFrameworkCore;
using SmartSure.Identity.Domain.Entities;
using MassTransit;

namespace SmartSure.Identity.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the Identity service.
/// Owns all identity-related tables: Users, Roles, UserRoles, Passwords, ExternalLogins, OtpRecords.
/// Also registers MassTransit Inbox/Outbox tables to support the transactional outbox pattern.
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    // DbSets expose each table for LINQ queries and change tracking
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;      // join table: Users ↔ Roles
    public DbSet<Password> Passwords { get; set; } = null!;       // BCrypt-hashed passwords (separate from User for auditability)
    public DbSet<ExternalLogin> ExternalLogins { get; set; } = null!; // OAuth providers linked to a user
    public DbSet<OtpRecord> OtpRecords { get; set; } = null!;    // temporary OTPs for forgot-password flow


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Register MassTransit Inbox/Outbox tables — required for transactional outbox pattern
        // These tables ensure messages are reliably delivered to RabbitMQ even if the broker is temporarily down.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // User table configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();  // enforce one account per email
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.VerificationToken).HasMaxLength(150);
        });

        // Role table configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique(); // role names must be unique
        });

        // UserRole table — many-to-many join between Users and Roles
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique(); // prevent duplicate role assignments
        });

        // Password table — one or more password records per user (newest is authoritative)
        modelBuilder.Entity<Password>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.HasOne(e => e.User).WithMany(u => u.Passwords).HasForeignKey(e => e.UserId);
        });

        // ExternalLogin table — stores OAuth provider + provider user ID for linked accounts
        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProviderKey).HasMaxLength(100).IsRequired();
            entity.HasOne(e => e.User).WithMany(u => u.ExternalLogins).HasForeignKey(e => e.UserId);
        });

        // OtpRecord table — one record per email; old records are deleted before new ones are inserted
        modelBuilder.Entity<OtpRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
            entity.Property(e => e.HashedOtp).HasMaxLength(500).IsRequired();
        });

    }
}
