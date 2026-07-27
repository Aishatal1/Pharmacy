namespace Pharmacy.Dtos;

public record InvoiceItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal PriceAtSale,
    decimal Total
);