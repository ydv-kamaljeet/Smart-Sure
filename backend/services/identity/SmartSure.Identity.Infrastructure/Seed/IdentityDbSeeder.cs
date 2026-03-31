using Microsoft.EntityFrameworkCore;
using SmartSure.Identity.Domain.Entities;
using SmartSure.Identity.Infrastructure.Data;

namespace SmartSure.Identity.Infrastructure.Seed;

/// <summary>
/// Static seeder that runs at application startup (before the first request is served).
/// Responsibilities:
///   1. Apply any pending EF Core migrations so the DB schema is always up-to-date.
///   2. Seed the two default roles: "Admin" and "Policyholder".
///   3. Seed a default admin user if none exists.
/// All operations are idempotent — safe to run on every restart.
/// </summary>
public static class IdentityDbSeeder
{
    public static async Task SeedAsync(IdentityDbContext context)
    {
        // Apply any pending migrations (safe if DB already up-to-date)
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            // Ensure the database exists if no migrations have run yet
            await context.Database.EnsureCreatedAsync();
        }

        // Seed roles only if the Roles table is empty — avoids duplicate inserts on restart
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "Policyholder" }
            );
            await context.SaveChangesAsync();
        }

        // Seed a default admin user if one doesn't already exist
        // Credentials: admin@smartsure.com / Admin@123
        // Password is BCrypt-hashed before storage — never stored in plain text
        if (!await context.Users.AnyAsync(u => u.Email == "admin@smartsure.com"))
        {
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var admin = new User
            {
                UserId = Guid.NewGuid(),
                Email = "admin@smartsure.com",
                FullName = "System Administrator",
                IsEmailVerified = true,  // seeded admin skips email verification
                IsActive = true
            };
            admin.Passwords.Add(new Password
            {
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123")
            });
            admin.UserRoles.Add(new UserRole { RoleId = adminRole.RoleId });

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
