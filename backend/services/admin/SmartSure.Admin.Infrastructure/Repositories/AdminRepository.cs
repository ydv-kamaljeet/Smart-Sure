using Microsoft.EntityFrameworkCore;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Admin.Infrastructure.Data;
using SmartSure.Admin.Application.Interfaces;

namespace SmartSure.Admin.Infrastructure.Repositories;

public class AdminRepository<T> : IAdminRepository<T> where T : class
{
    private readonly AdminDbContext _context;
    private readonly DbSet<T> _dbSet;

    public AdminRepository(AdminDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null) _dbSet.Remove(entity);
    }
}
