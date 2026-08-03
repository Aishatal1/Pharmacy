namespace Pharmacy.Dtos;

public record TransactionDto(
    int Id,
    string TransactionType,
    decimal Amount,
    string? Notes,
    DateTime CreatedAt,
    string CreatedByUsername
);