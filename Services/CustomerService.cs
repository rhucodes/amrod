using AmrodAssessment.Data;
using AmrodAssessment.Models;
using Microsoft.EntityFrameworkCore;

namespace AmrodAssessment.Services;

public interface ICustomerService
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByEmailAsync(string email);
    Task<Customer> CreateAsync(Customer customer);
}

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Customer?> GetByIdAsync(Guid id) =>
        await _db.Customers.FindAsync(id);

    public async Task<Customer?> GetByEmailAsync(string email) =>
        await _db.Customers.FirstOrDefaultAsync(c => c.Email == email.ToLower());

    public async Task<Customer> CreateAsync(Customer customer)
    {
        customer.Email     = customer.Email.ToLower();
        customer.CreatedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }
}