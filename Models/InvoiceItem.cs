namespace Pharmacy.Models;

public class InvoiceItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtSale { get; set; }
    public decimal Total { get; private set; }

    public int InvoiceId { get; set; }
    public int ProductId { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}