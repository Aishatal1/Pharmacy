using Microsoft.EntityFrameworkCore;
using Pharmacy.Models;


namespace Pharmacy.Data;
public class PharmaContext(DbContextOptions<PharmaContext> options) : 
DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

         modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

         modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.EmailAddress).HasMaxLength(100);
            entity.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

             entity.HasOne(c => c.CreatedBy)
                .WithMany(u => u.Customers)
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
         modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Barcode).IsUnique();
            entity.Property(p => p.Barcode).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.CompanyName).HasMaxLength(100);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(p => p.CreatedBy)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.InvoiceNumber).IsUnique();
            entity.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(i => i.TotalAmount).HasPrecision(18, 2);
            entity.Property(i => i.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.CreatedBy)
                .WithMany(u => u.Invoices)
                .HasForeignKey(i => i.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(ii => ii.Id);
            entity.Property(ii => ii.Quantity).IsRequired();
            entity.Property(ii => ii.PriceAtSale).HasPrecision(18, 2);
            entity.Property(ii => ii.Total)
                .HasComputedColumnSql("Quantity * PriceAtSale", stored: true);

            entity.HasOne(ii => ii.Invoice)
                .WithMany(i => i.InvoiceItems)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ii => ii.Product)
                .WithMany(p => p.InvoiceItems)
                .HasForeignKey(ii => ii.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            

                entity.Property(ii => ii.Total)
        .HasComputedColumnSql("Quantity * PriceAtSale", stored: true);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasOne(t => t.CreatedBy)
    .WithMany(u => u.Transactions)
    .HasForeignKey(t => t.CreatedByUserId)
    .OnDelete(DeleteBehavior.Restrict);
    
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TransactionType).IsRequired().HasMaxLength(50);
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.Notes).HasMaxLength(500);
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(t => t.Invoice)
                .WithMany(ii => ii.Transactions)
                .HasForeignKey(t => t.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Customer)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(al => al.Id);
            entity.Property(al => al.Action).IsRequired().HasMaxLength(100);
            entity.Property(al => al.TableName).IsRequired().HasMaxLength(100);
            entity.Property(al => al.Details).HasMaxLength(500);
            entity.Property(al => al.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(al => al.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    

    }
}