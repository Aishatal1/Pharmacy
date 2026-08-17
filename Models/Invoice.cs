namespace Pharmacy.Models;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }

    public int CustomerId { get; set; }
    public int CreatedByUserId { get; set; }
    public string ? Remarks {get;set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public List<Transaction> Transactions{get; set;}= new();
    public List<InvoiceItem> InvoiceItems { get; set; } = new();

}