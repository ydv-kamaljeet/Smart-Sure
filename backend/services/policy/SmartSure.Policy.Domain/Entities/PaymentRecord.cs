namespace SmartSure.Policy.Domain.Entities;

public class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // CreditCard, BankTransfer
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed"; // Completed, Failed, Pending

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public Policy? Policy { get; set; }
}
