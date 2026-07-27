namespace Pharmacy.Dtos;

public record TransactionDto(
    int Id,
    int InvoiceItemId,
    string TransactionType,
    decimal Amount,
    string? Notes,
    DateTime CreatedAt,
    string CreatedByUsername
);