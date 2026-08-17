using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Dtos;

public record CreateInvoiceDto(
    [Required] int CustomerId,
    [Required] [MinLength(1)] List<CreateInvoiceItemDto> Items,
    DateTime CreatedAt,
    string Remarks
);