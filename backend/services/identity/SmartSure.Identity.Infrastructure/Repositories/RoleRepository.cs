using Microsoft.EntityFrameworkCore;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Identity.Domain.Entities;
using SmartSure.Identity.Infrastructure.Data;

namespace SmartSure.Identity.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
    }
}
