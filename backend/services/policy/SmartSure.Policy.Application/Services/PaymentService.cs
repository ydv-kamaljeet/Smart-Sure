using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Shared.Common.Models;
using MassTransit;
using SmartSure.Shared.Contracts.Events;
using Razorpay.Api;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace SmartSure.Policy.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IPolicyManagementService _policyManagementService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly string _razorpayKeyId;
    private readonly string _razorpayKeySecret;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPolicyRepository policyRepository,
        IPolicyManagementService policyManagementService,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration)
    {
        _paymentRepository = paymentRepository;
        _policyRepository = policyRepository;
        _policyManagementService = policyManagementService;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _razorpayKeyId = configuration["Razorpay:KeyId"]!;
        _razorpayKeySecret = configuration["Razorpay:KeySecret"]!;
    }

    public async Task<PagedResult<PaymentRecordDto>> GetPaymentsAsync(Guid policyId, Guid userId, int page, int pageSize)
    {
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
        await _unitOfWork.SaveChangesAsync();

        try { await _publishEndpoint.Publish(new PremiumPaidEvent(policyId, dto.Amount, DateTime.UtcNow)); }
        catch { /* RabbitMQ unavailable — event skipped */ }

        return Result<PaymentRecordDto>.Success(new PaymentRecordDto(payment.Id, payment.PolicyId, payment.Amount, payment.PaymentMethod, payment.TransactionId, payment.Status, payment.PaidAt));
    }

    public Task<Result<RazorpayOrderResponseDto>> CreateRazorpayOrderAsync(decimal amount)
    {
        try
        {
            var client = new RazorpayClient(_razorpayKeyId, _razorpayKeySecret);
            // Razorpay expects amount in smallest currency unit (paise for INR)
            var options = new Dictionary<string, object>
            {
                { "amount", (int)(amount * 100) },
                { "currency", "INR" },
                { "receipt", $"rcpt_{Guid.NewGuid().ToString()[..8]}" }
            };

            var order = client.Order.Create(options);
            var orderId = order["id"].ToString();

            return Task.FromResult(Result<RazorpayOrderResponseDto>.Success(
                new RazorpayOrderResponseDto(orderId!, amount, "INR", _razorpayKeyId)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<RazorpayOrderResponseDto>.Failure($"Failed to create Razorpay order: {ex.Message}"));
        }
    }

    public async Task<Result<Guid>> VerifyAndCompletePaymentAsync(Guid userId, VerifyRazorpayPaymentDto dto)
    {
        // Verify Razorpay signature: HMAC-SHA256 of "orderId|paymentId" using KeySecret
        var payload = $"{dto.RazorpayOrderId}|{dto.RazorpayPaymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_razorpayKeySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var generatedSignature = Convert.ToHexString(hash).ToLower();

        if (generatedSignature != dto.RazorpaySignature)
            return Result<Guid>.Failure("Payment verification failed. Invalid signature.");

        // Create the policy now that payment is verified
        var buyDto = new BuyPolicyDto(dto.SubTypeId, dto.HomeDetails, dto.VehicleDetails);
        var policyResult = await _policyManagementService.BuyPolicyAsync(userId, buyDto);
        if (!policyResult.IsSuccess)
            return Result<Guid>.Failure(policyResult.ErrorMessage ?? "Failed to create policy.");

        // Record the payment against the new policy
        var payment = new Domain.Entities.PaymentRecord
        {
            Id = Guid.NewGuid(),
            PolicyId = policyResult.Data,
            Amount = 0, // Will be updated below from Razorpay
            PaymentMethod = "Razorpay",
            TransactionId = dto.RazorpayPaymentId,
            Status = "Completed"
        };

        // Fetch actual amount from Razorpay
        try
        {
            var client = new RazorpayClient(_razorpayKeyId, _razorpayKeySecret);
            var razorPayment = client.Payment.Fetch(dto.RazorpayPaymentId);
            payment.Amount = Convert.ToDecimal(razorPayment["amount"].ToString()) / 100m;
        }
        catch { /* amount stays 0 if fetch fails — non-critical */ }

        await _paymentRepository.AddPaymentAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        try { await _publishEndpoint.Publish(new PremiumPaidEvent(policyResult.Data, payment.Amount, DateTime.UtcNow)); }
        catch { /* RabbitMQ unavailable — event skipped, payment still succeeds */ }

        return Result<Guid>.Success(policyResult.Data);
    }
}
