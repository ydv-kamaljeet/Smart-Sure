using System.Threading.Tasks;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Infrastructure.Data;

namespace SmartSure.Claims.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ClaimsDbContext _context;

    public UnitOfWork(ClaimsDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
