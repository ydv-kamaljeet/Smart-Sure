using SmartSure.Policy.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Application.Interfaces;

public interface IPaymentService
{
    Task<PagedResult<PaymentRecordDto>> GetPaymentsAsync(Guid policyId, Guid userId, int page, int pageSize);
    Task<Result<PaymentRecordDto>> RecordPaymentAsync(Guid policyId, Guid userId, CreatePaymentDto dto);
}
