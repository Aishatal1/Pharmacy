using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Pharmacy.Endpoints;

public static class BillEndpoints
{
    public static void MapBillEndpoints(this WebApplication app)
    {
        //get all bills for a specific customer
        app.MapGet("/customers/{customerId}/bills", async (
            int customerId,
            bool includePaid,
            DateTime? startDate,
            DateTime? endDate,
            PharmaContext context) =>
        {
            var customerExists = await context.Customers.AnyAsync(c => c.Id == customerId);
            if (!customerExists)
            {
                return Results.NotFound($"Customer with ID {customerId} not found");
            }

            //build query
            var query = context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.CreatedBy)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Product)
                .Include(i => i.Transactions)  // ← FIXED: Load transactions directly
                    .ThenInclude(t => t.CreatedBy)
                .Where(i => i.CustomerId == customerId);

            if (!includePaid) 
            {
                query = query.Where(i => !i.IsPaid);
            }

            if (startDate.HasValue)
            {
                query = query.Where(i => i.CreatedAt >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(i => i.CreatedAt <= endDate.Value.Date.AddDays(1));
            }

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new InvoiceDto(
                    i.Id,
                    i.InvoiceNumber,
                    i.CustomerId,
                    i.Customer.Name,
                    i.TotalAmount,
                    i.IsPaid,
                    i.Remarks,
                    i.CreatedAt,
                    i.CreatedBy.FullName,
                    i.InvoiceItems.Select(ii => new InvoiceItemDto(
                        ii.Id,
                        ii.ProductId,
                        ii.Product.Name,
                        ii.Quantity,
                        ii.PriceAtSale,
                        ii.Total
                    )).ToList()
                ))
                .ToListAsync();

            var totalAmount = invoices.Sum(i => i.TotalAmount);
            
            var invoiceIds = invoices.Select(i => i.Id).ToList();
            
            var totalPaid = await context.Transactions
                .Where(t => invoiceIds.Contains(t.InvoiceId))
                .Where(t => t.TransactionType == "Payment")
                .SumAsync(t => t.Amount);

            var summary = new
            {
                TotalInvoices = invoices.Count,
                TotalAmount = totalAmount,
                TotalPaid = totalPaid,
                RemainingBalance = totalAmount - totalPaid,
                PaidInvoices = invoices.Count(i => i.IsPaid),
                UnpaidInvoices = invoices.Count(i => !i.IsPaid)
            };

            return Results.Ok(new
            {
                CustomerId = customerId,
                Invoices = invoices,
                Summary = summary
            });
        })
        .WithName("GetCustomerBills");

        //get summary of customer bills
        app.MapGet("/customers/{customerId}/bills/summary", async (
            int customerId,
            PharmaContext context) =>
        {
            var customer = await context.Customers
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
            {
                return Results.NotFound($"Customer with ID {customerId} not found");
            }

            var invoices = customer.Invoices;
            var totalAmount = invoices.Sum(i => i.TotalAmount);

            var invoiceIds = invoices.Select(i => i.Id).ToList();
            
            var totalPaid = await context.Transactions
                .Where(t => invoiceIds.Contains(t.InvoiceId))
                .Where(t => t.TransactionType == "Payment")
                .SumAsync(t => t.Amount);

            return Results.Ok(new
            {
                Customer = new
                {
                    customer.Id,
                    customer.Name,
                    customer.PhoneNumber,
                    customer.EmailAddress
                },
                InvoiceSummary = new
                {
                    TotalInvoices = invoices.Count,
                    TotalAmount = totalAmount,
                    TotalPaid = totalPaid,
                    RemainingBalance = totalAmount - totalPaid,
                    PaidInvoices = invoices.Count(i => i.IsPaid),
                    UnpaidInvoices = invoices.Count(i => !i.IsPaid)
                }
            });
        })
        .WithName("GetCustomerInvoiceSummary");

    }
}