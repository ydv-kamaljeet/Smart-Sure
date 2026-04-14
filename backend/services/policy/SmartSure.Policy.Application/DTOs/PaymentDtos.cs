namespace SmartSure.Policy.Application.DTOs;

public record PaymentRecordDto(Guid Id, Guid PolicyId, decimal Amount, string PaymentMethod, string TransactionId, string Status, DateTime PaidAt);

public record CreatePaymentDto(decimal Amount, string PaymentMethod, string TransactionId);

// Razorpay
public record CreateRazorpayOrderDto(decimal Amount, string Currency = "INR");

public record RazorpayOrderResponseDto(string OrderId, decimal Amount, string Currency, string KeyId);

public record VerifyRazorpayPaymentDto(
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature,
    // Policy details to create after payment
    int SubTypeId,
    CreateVehicleDetailsDto? VehicleDetails,
    CreateHomeDetailsDto? HomeDetails
);
