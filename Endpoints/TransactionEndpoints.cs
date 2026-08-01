using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Dtos;
using Pharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Pharmacy.Endpoints;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this WebApplication app)
    {
        // GET all transactions for an invoice
        app.MapGet("/invoices/{invoiceId}/transactions", async (
            int invoiceId,
            PharmaContext context) =>
        {
            var invoice = await context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                return Results.NotFound($"Invoice {invoiceId} not found");

            var invoiceItemIds = invoice.InvoiceItems.Select(ii => ii.Id).ToList();

            var transactions = await context.Transactions
                .Include(t => t.CreatedBy)
                .Where(t => invoiceItemIds.Contains(t.InvoiceItemId))
                .Select(t => new TransactionDto(
                    t.Id,
                    t.InvoiceItemId,
                    t.TransactionType,
                    t.Amount,
                    t.Notes,
                    t.CreatedAt,
                    t.CreatedBy.FullName
                ))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Results.Ok(transactions);
        });

        // POST create transaction (for refunds or manual payments)
        app.MapPost("/transactions", async (
            CreateTransactionDto createDto,
            ClaimsPrincipal user,
            PharmaContext context) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Results.Unauthorized();

            // Check if invoice item exists
            var invoiceItem = await context.InvoiceItems
                .Include(ii => ii.Invoice)
                .FirstOrDefaultAsync(ii => ii.Id == createDto.InvoiceItemId);

            if (invoiceItem == null)
                return Results.NotFound($"Invoice item {createDto.InvoiceItemId} not found");

            // Check if transaction type is valid
            var validTypes = new[] { "Sale", "Payment", "Refund" };
            if (!validTypes.Contains(createDto.TransactionType))
                return Results.BadRequest($"Transaction type must be one of: {string.Join(", ", validTypes)}");

            var transaction = new Transaction
            {
                InvoiceItemId = createDto.InvoiceItemId,
                TransactionType = createDto.TransactionType,
                Amount = createDto.Amount,
                Notes = createDto.Notes,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await context.Transactions.AddAsync(transaction);
            await context.SaveChangesAsync();

            // Update invoice paid status if this is a payment
            if (createDto.TransactionType == "Payment")
            {
                var invoice = invoiceItem.Invoice;
                var totalPaid = await context.Transactions
                    .Where(t => invoice.InvoiceItems.Select(ii => ii.Id).Contains(t.InvoiceItemId))
                    .Where(t => t.TransactionType == "Payment")
                    .SumAsync(t => t.Amount);

                if (totalPaid >= invoice.TotalAmount)
                {
                    invoice.IsPaid = true;
                    await context.SaveChangesAsync();
                }
            }

            var transactionDto = new TransactionDto(
                transaction.Id,
                transaction.InvoiceItemId,
                transaction.TransactionType,
                transaction.Amount,
                transaction.Notes,
                transaction.CreatedAt,
                user.FindFirst("FullName")?.Value ?? "Unknown"
            );

            return Results.Created($"/transactions/{transaction.Id}", new
            {
                Message = "Transaction created successfully",
                Transaction = transactionDto
            });
        });

        // GET transaction by ID
        app.MapGet("/transactions/{id}", async (
            int id,
            PharmaContext context) =>
        {
            var transaction = await context.Transactions
                .Include(t => t.CreatedBy)
                .Include(t => t.InvoiceItem)
                    .ThenInclude(ii => ii.Invoice)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                return Results.NotFound($"Transaction {id} not found");

            var transactionDto = new TransactionDto(
                transaction.Id,
                transaction.InvoiceItemId,
                transaction.TransactionType,
                transaction.Amount,
                transaction.Notes,
                transaction.CreatedAt,
                transaction.CreatedBy.FullName
            );

            return Results.Ok(transactionDto);
        });
    }
}
