namespace Pharmacy.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User CreatedBy { get; set; } = null!;
    public List<Invoice> Invoices { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();
}