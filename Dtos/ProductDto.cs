namespace Pharmacy.Dtos;

public record ProductDto(
    int Id,
    string Barcode,
    string Name,
    string CompanyName,
    DateOnly ProductionDate,
    DateOnly ExpirationDate,
    DateTime CreatedAt,
    string CreatedByUsername,
    Decimal Price
);