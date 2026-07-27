namespace Pharmacy.Dtos;

public record InvoiceDetailDto(
    int Id,
    string InvoiceNumber,
    DateTime CreatedAt,
    CustomerInfoDto Customer,
    UserInfoDto CreatedBy,
    decimal TotalAmount,
    bool IsPaid,
    List<InvoiceItemDetailDto> Items,
    PaymentSummaryDto PaymentSummary
);

public record CustomerInfoDto(
    int Id,
    string Name,
    string PhoneNumber,
    string EmailAddress
);

public record UserInfoDto(
    int Id,
    string FullName,
    string Username
);

public record InvoiceItemDetailDto(
    int Id,
    int ProductId,
    string ProductName,
    string Barcode,
    string CompanyName,
    int Quantity,
    decimal PriceAtSale,
    decimal Total,
    TransactionInfoDto? Transaction
);

public record TransactionInfoDto(
    int Id,
    string TransactionType,
    decimal Amount,
    string? Notes,
    DateTime CreatedAt,
    string CreatedByUsername
);

public record PaymentSummaryDto(
    decimal TotalPaid,
    decimal RemainingBalance,
    bool IsFullyPaid,
    List<TransactionInfoDto> Payments
);