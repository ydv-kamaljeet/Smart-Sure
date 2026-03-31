namespace SmartSure.Policy.Application.DTOs;

public record PaymentRecordDto(Guid Id, Guid PolicyId, decimal Amount, string PaymentMethod, string TransactionId, string Status, DateTime PaidAt);

public record CreatePaymentDto(decimal Amount, string PaymentMethod, string TransactionId);
