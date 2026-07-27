using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Dtos;

public record CreateCustomerDto(
    [Required] [StringLength(100)] string Name,
    [EmailAddress] string EmailAddress,
    [Required] [Phone] string PhoneNumber
);