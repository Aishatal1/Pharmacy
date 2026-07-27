using FluentValidation;

namespace Pharmacy.Dtos.Validators;

public class CreateInvoiceDtoValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Invoice must have at least one item");

        RuleForEach(x => x.Items).SetValidator(new CreateInvoiceItemDtoValidator());
    }
}