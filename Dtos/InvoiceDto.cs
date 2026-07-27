namespace Pharmacy.Dtos;

public record InvoiceDto(
    int Id,
    string InvoiceNumber,
    int CustomerId,
    string CustomerName,
    decimal TotalAmount,
    bool IsPaid,
    DateTime CreatedAt,
    string CreatedByUsername,
    List<InvoiceItemDto> Items
);