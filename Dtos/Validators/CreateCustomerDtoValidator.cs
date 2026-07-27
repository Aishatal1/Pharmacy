using FluentValidation;

namespace Pharmacy.Dtos.Validators;

public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerDtoValidator()
    {
        RuleFor(x => x.Name)
        .NotEmpty().WithMessage("Name is required!")
        .MaximumLength(100)
        .WithMessage("Name must not exceed 100 charrecters!");
        
        RuleFor(x => x.EmailAddress)
        .EmailAddress().WithMessage("Invalid email address!")
        .When(x => !string.IsNullOrEmpty(x.EmailAddress));

                RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters");
    }
}