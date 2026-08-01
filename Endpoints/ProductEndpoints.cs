using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Dtos;
using Pharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Pharmacy.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        // GET all products
        app.MapGet("/products", async (
            string? search,
            int page,
            int pageSize,
            PharmaContext context) =>
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = context.Products
                .Include(p => p.CreatedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    p.Name.Contains(search) || 
                    p.Barcode.Contains(search) ||
                    p.CompanyName.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto(
                    p.Id,
                    p.Barcode,
                    p.Name,
                    p.CompanyName,
                    p.ProductionDate,
                    p.ExpirationDate,
                    p.CreatedAt,
                    p.CreatedBy.FullName,
                    p.Price
                ))
                .ToListAsync();

            return Results.Ok(new
            {
                Data = products,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        });

        // GET product by ID
        app.MapGet("/products/{id}", async (int id, PharmaContext context) =>
        {
            var product = await context.Products
                .Include(p => p.CreatedBy)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (product == null)
                return Results.NotFound($"Product {id} not found");

            return Results.Ok(new ProductDto(
                product.Id,
                product.Barcode,
                product.Name,
                product.CompanyName,
                product.ProductionDate,
                product.ExpirationDate,
                product.CreatedAt,
                product.CreatedBy.FullName,
                product.Price
            ));
        });

        // POST create product
        app.MapPost("/products", async (
            CreateProductDto createDto,
            ClaimsPrincipal user,
            PharmaContext context) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Results.Unauthorized();

            var existing = await context.Products
                .FirstOrDefaultAsync(p => p.Barcode == createDto.Barcode);
            if (existing != null)
                return Results.Conflict($"Barcode '{createDto.Barcode}' already exists");

            var product = new Product
            {
                Barcode = createDto.Barcode,
                Name = createDto.Name,
                CompanyName = createDto.CompanyName ?? "",
                ProductionDate = createDto.ProductionDate,
                ExpirationDate = createDto.ExpirationDate,
                Price = createDto.Price,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await context.Products.AddAsync(product);
            await context.SaveChangesAsync();

            return Results.Created($"/products/{product.Id}", new
            {
                Message = "Product created successfully",
                ProductId = product.Id
            });
        });

        // PUT update product
        app.MapPut("/products/{id}", async (
            int id,
            CreateProductDto updateDto,
            ClaimsPrincipal user,
            PharmaContext context) =>
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
                return Results.NotFound($"Product {id} not found");

            var existing = await context.Products
                .FirstOrDefaultAsync(p => p.Barcode == updateDto.Barcode && p.Id != id);
            if (existing != null)
                return Results.Conflict($"Barcode '{updateDto.Barcode}' already exists");

            product.Barcode = updateDto.Barcode;
            product.Name = updateDto.Name;
            product.CompanyName = updateDto.CompanyName ?? "";
            product.ProductionDate = updateDto.ProductionDate;
            product.ExpirationDate = updateDto.ExpirationDate;
            product.Price = updateDto.Price;

            await context.SaveChangesAsync();

            return Results.Ok(new { Message = "Product updated successfully" });
        });

        // DELETE product
        app.MapDelete("/products/{id}", async (int id, PharmaContext context) =>
        {
            var product = await context.Products
                .Include(p => p.InvoiceItems)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (product == null)
                return Results.NotFound($"Product {id} not found");

            if (product.InvoiceItems.Any())
                return Results.BadRequest("Cannot delete product with existing invoice items");

            context.Products.Remove(product);
            await context.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
