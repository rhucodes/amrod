using Xunit;
using AmrodAssessment.Data;
using AmrodAssessment.Models;
using AmrodAssessment.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AmrodAssessment.Tests;

public class OrderServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new OrderService(_db);
    }

    private async Task<(Customer customer, Product product)> SeedBasicData(int stock = 50)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Dominic",
            LastName = "Marule",
            Email = "dominic@example.com",
            Phone = "0670217146"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 199.99m,
            Stock = stock,
            Description = "A product",
            ImageUrl = ""
        };

        await _db.Customers.AddAsync(customer);
        await _db.Products.AddAsync(product);
        await _db.SaveChangesAsync();

        return (customer, product);
    }

    // Place order successfully
    [Fact]
    public async Task CreateAsync_CreatesOrder_Successfully()
    {
        var (customer, product) = await SeedBasicData();

        var items = new List<OrderRequestItem> { new(product.Id, 2) };

        var (order, error) = await _service.CreateAsync(customer.Id, items, "Test order");

        error.Should().BeNull();
        order.Should().NotBeNull();
        order!.TotalAmount.Should().Be(399.98m);
        order.OrderItems.Should().HaveCount(1);
        order.Status.Should().Be(OrderStatus.Pending);
    }

    // Stock decrements on order
    [Fact]
    public async Task CreateAsync_DecrementsStock_OnSuccess()
    {
        var (customer, product) = await SeedBasicData(stock: 50);

        var items = new List<OrderRequestItem> { new(product.Id, 5) };

        await _service.CreateAsync(customer.Id, items, null);

        var updatedProduct = await _db.Products.FindAsync(product.Id);
        updatedProduct!.Stock.Should().Be(45);
    }

    // Cannot order more than available stock
    [Fact]
    public async Task CreateAsync_ReturnsError_WhenInsufficientStock()
    {
        var (customer, product) = await SeedBasicData(stock: 2);

        var items = new List<OrderRequestItem> { new(product.Id, 10) };

        var (order, error) = await _service.CreateAsync(customer.Id, items, null);

        order.Should().BeNull();
        error.Should().NotBeNull();
        error.Should().Contain("Insufficient stock");
    }

    // Cannot order with invalid customer
    [Fact]
    public async Task CreateAsync_ReturnsError_WhenCustomerNotFound()
    {
        var (_, product) = await SeedBasicData();

        var items = new List<OrderRequestItem> { new(product.Id, 1) };

        var (order, error) = await _service.CreateAsync(Guid.NewGuid(), items, null);

        order.Should().BeNull();
        error.Should().Be("Customer not found.");
    }

    // Price is snapshotted at time of order
    [Fact]
    public async Task CreateAsync_SnapshotsUnitPrice_AtTimeOfOrder()
    {
        var (customer, product) = await SeedBasicData();
        var originalPrice = product.Price;

        var items = new List<OrderRequestItem> { new(product.Id, 1) };
        var (order, _) = await _service.CreateAsync(customer.Id, items, null);

        product.Price = 999.99m;
        await _db.SaveChangesAsync();

        order!.OrderItems.First().UnitPrice.Should().Be(originalPrice);
    }

    // PayFast ITN updates order to Confirmed
    [Fact]
    public async Task UpdateStatusAsync_UpdatesTo_Confirmed_OnPayment()
    {
        var (customer, product) = await SeedBasicData();
        var items = new List<OrderRequestItem> { new(product.Id, 1) };
        var (order, _) = await _service.CreateAsync(customer.Id, items, null);

        var updated = await _service.UpdateStatusAsync(order!.Id, OrderStatus.Confirmed);

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(OrderStatus.Confirmed);
    }

    // PayFast ITN updates order to Cancelled
    [Fact]
    public async Task UpdateStatusAsync_UpdatesTo_Cancelled_OnFailure()
    {
        var (customer, product) = await SeedBasicData();
        var items = new List<OrderRequestItem> { new(product.Id, 1) };
        var (order, _) = await _service.CreateAsync(customer.Id, items, null);

        var updated = await _service.UpdateStatusAsync(order!.Id, OrderStatus.Cancelled);

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(OrderStatus.Cancelled);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}