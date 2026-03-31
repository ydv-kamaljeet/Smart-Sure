using Microsoft.EntityFrameworkCore;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Policy.Domain.Entities;
using SmartSure.Policy.Infrastructure.Data;

namespace SmartSure.Policy.Infrastructure.Repositories;

public class InsuranceCatalogRepository : IInsuranceCatalogRepository
{
    private readonly PolicyDbContext _context;

    public InsuranceCatalogRepository(PolicyDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InsuranceType>> GetAllTypesAsync(bool activeOnly = true)
    {
        var query = _context.InsuranceTypes.AsQueryable();
        if (activeOnly) query = query.Where(t => t.IsActive);
        return await query.ToListAsync();
    }

    public async Task<InsuranceType?> GetTypeByIdAsync(int id)
    {
        return await _context.InsuranceTypes
                             .Include(t => t.SubTypes)
                             .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<InsuranceType?> GetTypeByNameAsync(string name)
    {
        return await _context.InsuranceTypes.FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task AddTypeAsync(InsuranceType type)
    {
        await _context.InsuranceTypes.AddAsync(type);
    }

    public Task UpdateTypeAsync(InsuranceType type)
    {
        _context.InsuranceTypes.Update(type);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<InsuranceSubType>> GetSubTypesByTypeIdAsync(int typeId, bool activeOnly = true)
    {
        var query = _context.InsuranceSubTypes.Where(s => s.InsuranceTypeId == typeId);
        if (activeOnly) query = query.Where(s => s.IsActive);
        return await query.ToListAsync();
    }

    public async Task<InsuranceSubType?> GetSubTypeByIdAsync(int id)
    {
        return await _context.InsuranceSubTypes
                             .Include(s => s.InsuranceType)
                             .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddSubTypeAsync(InsuranceSubType subType)
    {
        await _context.InsuranceSubTypes.AddAsync(subType);
    }

    public Task UpdateSubTypeAsync(InsuranceSubType subType)
    {
        _context.InsuranceSubTypes.Update(subType);
        return Task.CompletedTask;
    }
}
