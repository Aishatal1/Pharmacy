using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Dtos;

public record CreateInvoiceItemDto(
    [Required] int ProductId,
    [Required] [Range(1, int.MaxValue)] int Quantity,
    [Required] [Range(0.01, double.MaxValue)] decimal PriceAtSale
);