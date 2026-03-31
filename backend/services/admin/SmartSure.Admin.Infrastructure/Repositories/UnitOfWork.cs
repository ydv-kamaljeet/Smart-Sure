using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Infrastructure.Data;

namespace SmartSure.Admin.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AdminDbContext _context;

    public UnitOfWork(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
