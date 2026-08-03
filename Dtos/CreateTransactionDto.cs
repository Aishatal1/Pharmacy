using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Dtos;

public record CreateTransactionDto(
    [Required] string TransactionType,
    [Required] [Range(0.01, double.MaxValue)] decimal Amount,
    string? Notes,
    int? InvoiceId
);