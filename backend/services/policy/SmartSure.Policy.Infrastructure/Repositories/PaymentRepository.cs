using Microsoft.EntityFrameworkCore;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Policy.Domain.Entities;
using SmartSure.Policy.Infrastructure.Data;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PolicyDbContext _context;

    public PaymentRepository(PolicyDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PaymentRecord>> GetPaymentsByPolicyIdAsync(Guid policyId, int page, int pageSize)
    {
        var query = _context.PaymentRecords.Where(p => p.PolicyId == policyId);
        
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.PaidAt)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return new PagedResult<PaymentRecord>
        {
            TotalCount = total,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task AddPaymentAsync(PaymentRecord payment)
    {
        await _context.PaymentRecords.AddAsync(payment);
    }
}
