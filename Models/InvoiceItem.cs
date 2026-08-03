namespace Pharmacy.Models;

public class InvoiceItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtSale { get; set; }
    public decimal Total { get; set; }  // Changed from private set to public set

    public int InvoiceId { get; set; }
    public int ProductId { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
