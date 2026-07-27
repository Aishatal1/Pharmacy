namespace Pharmacy.Models;

public class Transaction
{
    public int Id { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Sale, Payment, Refund
    public decimal Amount { get; set; }
    public string? Notes { get; set; }

    public int InvoiceItemId { get; set; }
    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public InvoiceItem InvoiceItem { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}