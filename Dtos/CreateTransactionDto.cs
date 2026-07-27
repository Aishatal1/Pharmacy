using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Dtos;

public record CreateTransactionDto(
    [Required] int InvoiceItemId,
    [Required] string TransactionType,
    [Required] [Range(0.01, double.MaxValue)] decimal Amount,
    string? Notes
);