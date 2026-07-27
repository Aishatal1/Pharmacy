namespace Pharmacy.Models;

public class Product
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateOnly ProductionDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public Decimal Price {get;set;}

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User CreatedBy { get; set; } = null!;
    public List<InvoiceItem> InvoiceItems { get; set; } = new();
}