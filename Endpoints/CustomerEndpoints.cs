using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Dtos;
using Pharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Pharmacy.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        // GET all customers
        app.MapGet("/customers", async (
            string? search,
            int page,
            int pageSize,
            PharmaContext context) =>
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = context.Customers
                .Include(c => c.CreatedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => 
                    c.Name.Contains(search) || 
                    c.PhoneNumber.Contains(search) ||
                    c.EmailAddress.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerDto(
                    c.Id,
                    c.Name,
                    c.EmailAddress,
                    c.PhoneNumber,
                    c.CreatedAt,
                    c.CreatedBy.FullName
                ))
                .ToListAsync();

            return Results.Ok(new
            {
                Data = customers,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        });

        // GET customer by ID
        app.MapGet("/customers/{id}", async (int id, PharmaContext context) =>
        {
            var customer = await context.Customers
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (customer == null)
                return Results.NotFound($"Customer {id} not found");

            return Results.Ok(new CustomerDto(
                customer.Id,
                customer.Name,
                customer.EmailAddress,
                customer.PhoneNumber,
                customer.CreatedAt,
                customer.CreatedBy.FullName
            ));
        }).RequireAuthorization();

        // POST create customer
        app.MapPost("/customers", async (
            CreateCustomerDto createDto,
            ClaimsPrincipal user,
            PharmaContext context) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Results.Unauthorized();

            var existing = await context.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == createDto.PhoneNumber);
            if (existing != null)
                return Results.Conflict($"Phone number '{createDto.PhoneNumber}' already exists");

            var customer = new Customer
            {
                Name = createDto.Name,
                EmailAddress = createDto.EmailAddress ?? "",
                PhoneNumber = createDto.PhoneNumber,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();

            return Results.Created($"/customers/{customer.Id}", new
            {
                Message = "Customer created successfully",
                CustomerId = customer.Id
            });
        });

        // PUT update customer
        app.MapPut("/customers/{id}", async (
            int id,
            CreateCustomerDto updateDto,
            ClaimsPrincipal user,
            PharmaContext context) =>
        {
            var customer = await context.Customers.FindAsync(id);
            if (customer == null)
                return Results.NotFound($"Customer {id} not found");

            var existing = await context.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == updateDto.PhoneNumber && c.Id != id);
            if (existing != null)
                return Results.Conflict($"Phone number '{updateDto.PhoneNumber}' already exists");

            customer.Name = updateDto.Name;
            customer.EmailAddress = updateDto.EmailAddress ?? "";
            customer.PhoneNumber = updateDto.PhoneNumber;

            await context.SaveChangesAsync();

            return Results.Ok(new { Message = "Customer updated successfully" });
        });

        // DELETE customer
        app.MapDelete("/customers/{id}", async (int id, PharmaContext context) =>
        {
            var customer = await context.Customers
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (customer == null)
                return Results.NotFound($"Customer {id} not found");

            if (customer.Invoices.Any())
                return Results.BadRequest("Cannot delete customer with existing invoices");

            context.Customers.Remove(customer);
            await context.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
