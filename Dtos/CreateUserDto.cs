using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Dtos;

public record CreateUserDto(
    [Required] [StringLength(50)] string Username,
    [Required] [MinLength(6)] string Password,
    [Required] [StringLength(100)] string FullName,
    [Required] string Role
);