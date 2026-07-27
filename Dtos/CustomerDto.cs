namespace Pharmacy.Dtos;

public record CustomerDto(
    int Id,
    string Name,
    string EmailAddress,
    string PhoneNumber,
    DateTime CreatedAt,
    string CreatedByUsername
);