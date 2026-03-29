using Xunit;
using AmrodAssessment.Data;
using AmrodAssessment.Models;
using AmrodAssessment.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AmrodAssessment.Tests;

public class CustomerServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new CustomerService(_db);
    }

    [Fact]
    public async Task CreateAsync_CreatesCustomer_Successfully()
    {
        var customer = new Customer
        {
            FirstName = "Dominic",
            LastName = "Marule",
            Email = "dominic@example.com",
            Phone = "0670217146"
        };

        var result = await _service.CreateAsync(customer);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Email.Should().Be("dominic@example.com");
    }

    [Fact]
    public async Task CreateAsync_NormalisesEmail_ToLowercase()
    {
        var customer = new Customer
        {
            FirstName = "Musa",
            LastName = "Siwele",
            Email = "musa@EXAMPLE.COM",
            Phone = ""
        };

        var result = await _service.CreateAsync(customer);

        result.Email.Should().Be("musa@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsCustomer_WhenExists()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Kakanyo",
            LastName = "Sekgobela",
            Email = "kakanyo@example.com",
            Phone = ""
        };

        await _db.Customers.AddAsync(customer);
        await _db.SaveChangesAsync();

        var result = await _service.GetByEmailAsync("kakanyo@example.com");

        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Kakanyo");
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _service.GetByEmailAsync("nobody@example.com");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCustomer_WhenExists()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Mpho",
            LastName = "Morole",
            Email = "mpho@example.com",
            Phone = ""
        };

        await _db.Customers.AddAsync(customer);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(customer.Id);

        result.Should().NotBeNull();
        result!.Email.Should().Be("mpho@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}