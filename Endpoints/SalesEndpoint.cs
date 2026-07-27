using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Pharmacy.Endpoints;

public static class SalesEndpoints
{
    public static void MapSalesEndpoints(this WebApplication app)
    {
        // Get daily sales validation
        app.MapGet("/sales/daily-validation", async (
            [FromQuery] DateTime? date,
            PharmaContext context) =>
        {
            var targetDate = date ?? DateTime.UtcNow.Date;
            var startOfDay = targetDate.Date;
            var endOfDay = startOfDay.AddDays(1);

            // Get all invoices for today
            var invoices = await context.Invoices
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Product)
                .Include(i => i.Customer)
                .Where(i => i.CreatedAt >= startOfDay && i.CreatedAt < endOfDay)
                .ToListAsync();

            var validationMessages = new List<string>();
            var isValid = true;

            // Validation 1: Check for missing invoice items
            var invoicesWithNoItems = invoices.Where(i => !i.InvoiceItems.Any()).ToList();
            if (invoicesWithNoItems.Any())
            {
                isValid = false;
                validationMessages.Add($"Found {invoicesWithNoItems.Count} invoice(s) with no items");
            }

            // Validation 2: Check for mismatched totals
            foreach (var invoice in invoices)
            {
                var calculatedTotal = invoice.InvoiceItems.Sum(ii => ii.Total);
                if (Math.Abs(invoice.TotalAmount - calculatedTotal) > 0.01m)
                {
                    isValid = false;
                    validationMessages.Add($"Invoice #{invoice.InvoiceNumber} has mismatched total: Expected {calculatedTotal}, Actual {invoice.TotalAmount}");
                }
            }

            // Validation 3: Check for missing customer
            var invoicesWithNoCustomer = invoices.Where(i => i.CustomerId == 0 || i.Customer == null).ToList();
            if (invoicesWithNoCustomer.Any())
            {
                isValid = false;
                validationMessages.Add($"Found {invoicesWithNoCustomer.Count} invoice(s) with no customer");
            }

            // Validation 4: Check for zero or negative quantities
            var invalidItems = invoices
                .SelectMany(i => i.InvoiceItems)
                .Where(ii => ii.Quantity <= 0 || ii.PriceAtSale < 0)
                .ToList();
            
            if (invalidItems.Any())
            {
                isValid = false;
                validationMessages.Add($"Found {invalidItems.Count} invoice item(s) with invalid quantities or prices");
            }

            // Calculate summary statistics
            var totalInvoices = invoices.Count;
            var totalItemsSold = invoices.Sum(i => i.InvoiceItems.Sum(ii => ii.Quantity));
            var totalRevenue = invoices.Sum(i => i.TotalAmount);
            var averageInvoiceValue = totalInvoices > 0 ? totalRevenue / totalInvoices : 0;

            // Get top selling products
            var topProducts = invoices
                .SelectMany(i => i.InvoiceItems)
                .GroupBy(ii => new { ii.ProductId, ii.Product.Name })
                .Select(g => new SalesByProductDto(
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Sum(ii => ii.Quantity),
                    g.Sum(ii => ii.Total)
                ))
                .OrderByDescending(p => p.Revenue)
                .Take(5)
                .ToList();

            // Get sales by hour
            var salesByHour = invoices
                .GroupBy(i => i.CreatedAt.Hour)
                .Select(g => new SalesByHourDto(
                    g.Key,
                    g.Count(),
                    g.Sum(i => i.TotalAmount)
                ))
                .OrderBy(h => h.Hour)
                .ToList();

            var summary = new SalesSummaryDto(
                targetDate,
                totalInvoices,
                totalItemsSold,
                totalRevenue,
                averageInvoiceValue,
                topProducts,
                salesByHour,
                isValid,
                validationMessages
            );

            return Results.Ok(summary);
        })
        .WithName("GetDailySalesValidation")
        .WithOpenApi()
        .RequireAuthorization();

        // Get sales summary for a date range
        app.MapGet("/sales/range", async (
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            PharmaContext context) =>
        {
            if (startDate > endDate)
            {
                return Results.BadRequest("Start date must be before end date");
            }

            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            var dailyStats = await context.Invoices
                .Where(i => i.CreatedAt >= start && i.CreatedAt < end)
                .GroupBy(i => i.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalInvoices = g.Count(),
                    TotalRevenue = g.Sum(i => i.TotalAmount),
                    TotalItems = g.Sum(i => i.InvoiceItems.Sum(ii => ii.Quantity))
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            return Results.Ok(dailyStats);
        })
        .WithName("GetSalesRange")
        .WithOpenApi()
        .RequireAuthorization();
    }
}