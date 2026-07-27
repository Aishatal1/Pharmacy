namespace Pharmacy.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin, Cashier, Manager
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Customer> Customers { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<Invoice> Invoices { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();
    public List<ActivityLog> ActivityLogs { get; set; } = new();
}