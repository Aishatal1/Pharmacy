using FluentValidation;

namespace Pharmacy.Dtos.Validators;

public class CreateInvoiceItemDtoValidator : AbstractValidator<CreateInvoiceItemDto>
{
    public CreateInvoiceItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.PriceAtSale)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
    }
}