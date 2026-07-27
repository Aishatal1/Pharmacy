using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Dtos;

public record CreateProductDto(
    [Required] string Barcode,
    [Required] [StringLength(200)] string Name,
    [StringLength(100)] string CompanyName,
    DateOnly ProductionDate,
    DateOnly ExpirationDate,
    Decimal Price
);