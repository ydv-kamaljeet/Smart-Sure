using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Shared.Common.Models;
using MassTransit;
using System.Text.Json;
using SmartSure.Shared.Contracts.Events;

namespace SmartSure.Policy.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentService(
        IPaymentRepository paymentRepository, 
        IPolicyRepository policyRepository, 
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _paymentRepository = paymentRepository;
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<PagedResult<PaymentRecordDto>> GetPaymentsAsync(Guid policyId, Guid userId, int page, int pageSize)
    {
        // Enforce user ownership
        var policy = await _policyRepository.GetPolicyByIdAndUserIdAsync(policyId, userId);
        if (policy == null) return new PagedResult<PaymentRecordDto>();

        var result = await _paymentRepository.GetPaymentsByPolicyIdAsync(policyId, page, pageSize);
        
        var dtos = result.Items.Select(p => new PaymentRecordDto(p.Id, p.PolicyId, p.Amount, p.PaymentMethod, p.TransactionId, p.Status, p.PaidAt));

        return new PagedResult<PaymentRecordDto>
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Items = dtos
        };
    }

    public async Task<Result<PaymentRecordDto>> RecordPaymentAsync(Guid policyId, Guid userId, CreatePaymentDto dto)
    {
        var policy = await _policyRepository.GetPolicyByIdAndUserIdAsync(policyId, userId);
        if (policy == null) return Result<PaymentRecordDto>.Failure("Policy not found.");

        var payment = new Domain.Entities.PaymentRecord
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            TransactionId = dto.TransactionId,
            Status = "Completed"
        };

        await _paymentRepository.AddPaymentAsync(payment);

        await _publishEndpoint.Publish(new PremiumPaidEvent(
            policyId,
            dto.Amount,
            DateTime.UtcNow
        ));

        await _unitOfWork.SaveChangesAsync();

        return Result<PaymentRecordDto>.Success(new PaymentRecordDto(payment.Id, payment.PolicyId, payment.Amount, payment.PaymentMethod, payment.TransactionId, payment.Status, payment.PaidAt));
    }
}
