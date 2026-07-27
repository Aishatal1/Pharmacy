namespace Pharmacy.Dtos;

public record LoginResponseDto(
    string Token,
    UserDto User
);