using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Dtos;

public record LoginDto(
    [Required] string Username,
    [Required] string Password
);