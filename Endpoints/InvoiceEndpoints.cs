using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Dtos;
using Pharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Pharmacy.Endpoints;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this WebApplication app)
    {
        // GET all invoices
        app.MapGet("/invoices", async (
            int? customerId,
            bool? isPaid,
            int page,
            int pageSize,
            PharmaContext context) =>
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.CreatedBy)
                .Include(i => i.InvoiceItems)
                .AsQueryable();

            if (customerId.HasValue)
                query = query.Where(i => i.CustomerId == customerId.Value);

            if (isPaid.HasValue)
                query = query.Where(i => i.IsPaid == isPaid.Value);

            var totalCount = await query.CountAsync();

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return Results.Ok(new
            {
                Data = invoices,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        });

        // GET invoice by ID (with details)
        app.MapGet("/invoices/{id}/details", async (int id, PharmaContext context) =>
        {
            var invoice = await context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.CreatedBy)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Product)
                .Include(i => i.Transactions)
                    .ThenInclude(t => t.CreatedBy)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return Results.NotFound($"Invoice {id} not found");

            var customerInfo = new CustomerInfoDto(
                invoice.Customer.Id,
                invoice.Customer.Name,
                invoice.Customer.PhoneNumber,
                invoice.Customer.EmailAddress
            );

            var userInfo = new UserInfoDto(
                invoice.CreatedBy.Id,
                invoice.CreatedBy.FullName,
                invoice.CreatedBy.Username
            );

            var items = invoice.InvoiceItems.Select(ii =>
            {
                var transaction = invoice.Transactions.FirstOrDefault(t => t.TransactionType == "Payment");
                
                var transactionInfo = transaction != null ? new TransactionInfoDto(
                    transaction.Id,
                    transaction.TransactionType,
                    transaction.Amount,
                    transaction.Notes,
                    transaction.CreatedAt,
                    transaction.CreatedBy?.FullName ?? "Unknown"
                ) : null;

                return new InvoiceItemDetailDto(
                    ii.Id,
                    ii.ProductId,
                    ii.Product.Name,
                    ii.Product.Barcode,
                    ii.Product.CompanyName,
                    ii.Quantity,
                    ii.PriceAtSale,
                    ii.Total,
                    transactionInfo
                );
            }).ToList();

            var allPayments = invoice.Transactions
                .Where(t => t.TransactionType == "Payment")
                .ToList();

            var payments = allPayments.Select(t => new TransactionInfoDto(
                t.Id,
                t.TransactionType,
                t.Amount,
                t.Notes,
                t.CreatedAt,
                t.CreatedBy?.FullName ?? "Unknown"
            )).ToList();

            var totalPaid = payments.Sum(p => p.Amount);
            var remainingBalance = invoice.TotalAmount - totalPaid;

            var paymentSummary = new PaymentSummaryDto(
                totalPaid,
                remainingBalance,
                remainingBalance <= 0,
                payments
            );

            var invoiceDetail = new InvoiceDetailDto(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.CreatedAt,
                customerInfo,
                userInfo,
                invoice.TotalAmount,
                invoice.IsPaid,
                items,
                paymentSummary
            );

            return Results.Ok(invoiceDetail);
        });

        // POST create invoice (with items)
        app.MapPost("/invoices", async (
            CreateInvoiceDto createDto,
            ClaimsPrincipal user,
            PharmaContext context) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Results.Unauthorized();

            // Check if customer exists
            var customer = await context.Customers.FindAsync(createDto.CustomerId);
            if (customer == null)
                return Results.NotFound($"Customer {createDto.CustomerId} not found");

            // Generate invoice number
            var invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8)}";

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                CustomerId = createDto.CustomerId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsPaid = false,
                Remarks ="",
                InvoiceItems = new List<InvoiceItem>()
            };

            decimal totalAmount = 0;

            foreach (var itemDto in createDto.Items)
            {
                // Check if product exists
                var product = await context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                    return Results.NotFound($"Product {itemDto.ProductId} not found");

                var invoiceItem = new InvoiceItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    PriceAtSale = itemDto.PriceAtSale,
                    Total = itemDto.Quantity * itemDto.PriceAtSale
                };

                invoice.InvoiceItems.Add(invoiceItem);
                totalAmount += invoiceItem.Total;
            }

            invoice.TotalAmount = totalAmount;

            await context.Invoices.AddAsync(invoice);
            await context.SaveChangesAsync();

            var response = new
            {
                Message = "Invoice created successfully",
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                TotalAmount = invoice.TotalAmount
            };

            return Results.Created($"/invoices/{invoice.Id}", response);
        });

        // POST add payment to invoice
        app.MapPost("/invoices/{invoiceId}/pay", async (
            int invoiceId,
            [FromBody] decimal amount,
            ClaimsPrincipal user,
            PharmaContext context) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Results.Unauthorized();

            var invoice = await context.Invoices
                .Include(i => i.Customer)  
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                return Results.NotFound($"Invoice {invoiceId} not found");

               //DEBUG - Check the values
    Console.WriteLine($"=== DEBUG ===");
    Console.WriteLine($"Invoice ID: {invoice.Id}");
    Console.WriteLine($"CustomerId from Invoice: {invoice.CustomerId}");
    Console.WriteLine($"Customer exists: {invoice.Customer != null}");
    Console.WriteLine($"Customer Name: {invoice.Customer?.Name ?? "NULL"}");
    Console.WriteLine($"Amount: {amount}");
    Console.WriteLine($"User ID: {userId}");
    Console.WriteLine($"============");

            if (invoice.IsPaid)
                return Results.BadRequest("Invoice is already fully paid");

            if (amount <= 0)
                return Results.BadRequest("Amount must be greater than 0");

            var transaction = new Transaction
            {
                InvoiceId = invoice.Id,
                CustomerId = invoice.CustomerId,  
                TransactionType = "Payment",
                Amount = amount,
                Notes = $"Payment for invoice {invoice.InvoiceNumber}",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await context.Transactions.AddAsync(transaction);
            await context.SaveChangesAsync();

            // Check if invoice is now fully paid
            var totalPaid = await context.Transactions
                .Where(t => t.InvoiceId == invoice.Id)
                .Where(t => t.TransactionType == "Payment")
                .SumAsync(t => t.Amount);

            if (totalPaid >= invoice.TotalAmount)
            {
                invoice.IsPaid = true;
                await context.SaveChangesAsync();
            }

            return Results.Ok(new
            {
                Message = "Payment recorded successfully",
                Amount = amount,
                InvoiceId = invoice.Id,
                IsFullyPaid = invoice.IsPaid
            });
        });
    }
}