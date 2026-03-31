using SmartSure.Policy.Domain.Entities;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Application.Interfaces;

public interface IPaymentRepository
{
    Task<PagedResult<PaymentRecord>> GetPaymentsByPolicyIdAsync(Guid policyId, int page, int pageSize);
    Task AddPaymentAsync(PaymentRecord payment);
}
