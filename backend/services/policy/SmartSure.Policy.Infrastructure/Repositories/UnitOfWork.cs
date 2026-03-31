using SmartSure.Policy.Application.Interfaces;
using SmartSure.Policy.Infrastructure.Data;

namespace SmartSure.Policy.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly PolicyDbContext _context;

    public UnitOfWork(PolicyDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
