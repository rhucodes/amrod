using Xunit;
using AmrodAssessment.Data;
using AmrodAssessment.Models;
using AmrodAssessment.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AmrodAssessment.Tests;

public class ProductServiceTests : IDisposable
{
    private readonly AppDbContext  _db;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db      = new AppDbContext(options);
        _service = new ProductService(_db);
    }

    private async Task SeedProducts()
    {
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "T-Shirt",  Price = 149.99m, Stock = 100, Description = "A shirt",  ImageUrl = "" },
            new() { Id = Guid.NewGuid(), Name = "Hoodie",   Price = 449.99m, Stock = 50,  Description = "A hoodie", ImageUrl = "" },
            new() { Id = Guid.NewGuid(), Name = "Cap",      Price = 219.99m, Stock = 0,   Description = "A cap",    ImageUrl = "" },
            new() { Id = Guid.NewGuid(), Name = "Notebook", Price = 79.99m,  Stock = 200, Description = "A book",   ImageUrl = "" },
        };

        await _db.Products.AddRangeAsync(products);
        await _db.SaveChangesAsync();
    }

    // Catalog — load all products
    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts_WhenNoFilters()
    {
        await SeedProducts();

        var result = await _service.GetAllAsync(null, null, null, null, null, null);

        result.Should().HaveCount(4);
    }

    // Search by name
    [Fact]
    public async Task GetAllAsync_FiltersBy_SearchTerm()
    {
        await SeedProducts();

        var result = await _service.GetAllAsync("shirt", null, null, null, null, null);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("T-Shirt");
    }

    // Filter by min price
    [Fact]
    public async Task GetAllAsync_FiltersBy_MinPrice()
    {
        await SeedProducts();

        var result = await _service.GetAllAsync(null, 200m, null, null, null, null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Price >= 200m);
    }

    // Filter by max price
    [Fact]
    public async Task GetAllAsync_FiltersBy_MaxPrice()
    {
        await SeedProducts();

        var result = await _service.GetAllAsync(null, null, 200m, null, null, null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Price <= 200m);
    }

    // Filter by price range
    [Fact]
    public async Task GetAllAsync_FiltersBy_PriceRange()
    {
        await SeedProducts();

        var result = await _service.GetAllAsync(null, 100m, 300m, null, null, null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Price >= 100m && p.Price <= 300m);
    }

    // Filter in stock only
    [Fact]
    public async Task GetAllAsync_FiltersBy_InStock()
    {
        await SeedProducts();

        var result = await _service.GetAllAsync(null, null, null, true, null, null);

        result.Should().HaveCount(3);
        result.Should().OnlyContain(p => p.Stock > 0);
    }

    // Sort price low to high
    [Fact]
    public async Task GetAllAsync_SortsBy_PriceAscending()
    {
        await SeedProducts();

        var result = (await _service.GetAllAsync(null, null, null, null, "price", "asc")).ToList();

        result.Should().BeInAscendingOrder(p => p.Price);
    }

    // Sort price high to low
    [Fact]
    public async Task GetAllAsync_SortsBy_PriceDescending()
    {
        await SeedProducts();

        var result = (await _service.GetAllAsync(null, null, null, null, "price", "desc")).ToList();

        result.Should().BeInDescendingOrder(p => p.Price);
    }

    // Sort name A to Z
    [Fact]
    public async Task GetAllAsync_SortsBy_NameAscending()
    {
        await SeedProducts();

        var result = (await _service.GetAllAsync(null, null, null, null, "name", "asc")).ToList();

        result.Should().BeInAscendingOrder(p => p.Name);
    }

    // Product detail view
    [Fact]
    public async Task GetByIdAsync_ReturnsProduct_WhenExists()
    {
        var product = new Product
        {
            Id          = Guid.NewGuid(),
            Name        = "Test Product",
            Price       = 99.99m,
            Stock       = 10,
            Description = "Test",
            ImageUrl    = ""
        };

        await _db.Products.AddAsync(product);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Product");
    }

    // Product not found
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