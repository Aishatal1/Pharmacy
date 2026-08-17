using FluentValidation;

namespace Pharmacy.Dtos.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Barcode)
            .NotEmpty().WithMessage("Barcode is required")
            .MaximumLength(50).WithMessage("Barcode must not exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
        
         RuleFor(x => x.ExpirationDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.Now))
            .WithMessage("Expiration Date must be in the future!");
        
        RuleFor(x => x.ExpirationDate)
            .GreaterThan(x => x.ProductionDate)
            .WithMessage("Expiration Date must be after creation date!");
    }
}