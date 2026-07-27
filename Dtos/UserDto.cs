namespace Pharmacy.Dtos;

public record UserDto(
    int Id,
    string Username,
    string FullName,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);