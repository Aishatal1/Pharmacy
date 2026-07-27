namespace Pharmacy.Dtos;

public record SalesSummaryDto(
    DateTime Date,
    int TotalInvoices,
    int TotalItemsSold,
    decimal TotalRevenue,
    decimal AverageInvoiceValue,
    List<SalesByProductDto> TopProducts,
    List<SalesByHourDto> SalesByHour,
    bool IsValid,
    List<string> ValidationMessages
);

public record SalesByProductDto(
    int ProductId,
    string ProductName,
    int QuantitySold,
    decimal Revenue
);

public record SalesByHourDto(
    int Hour,
    int Invoices,
    decimal Revenue
);