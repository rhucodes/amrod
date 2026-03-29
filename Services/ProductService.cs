using AmrodAssessment.Data;
using AmrodAssessment.Models;
using Microsoft.EntityFrameworkCore;

namespace AmrodAssessment.Services;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllAsync(string? search, decimal? minPrice, decimal? maxPrice, bool? inStock, string? sortBy, string? sortOrder);
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product> CreateAsync(Product product);
    Task<Product?> UpdateAsync(Guid id, Product product);
    Task<bool> DeleteAsync(Guid id);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(
        string? search,
        decimal? minPrice,
        decimal? maxPrice,
        bool? inStock,
        string? sortBy,
        string? sortOrder)
    {
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()) ||
                                     p.Description.ToLower().Contains(search.ToLower()));

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        if (inStock.HasValue && inStock.Value)
            query = query.Where(p => p.Stock > 0);

        query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
        {
            ("price", "asc")  => query.OrderBy(p => p.Price),
            ("price", "desc") => query.OrderByDescending(p => p.Price),
            ("name",  "asc")  => query.OrderBy(p => p.Name),
            ("name",  "desc") => query.OrderByDescending(p => p.Name),
            ("stock", "asc")  => query.OrderBy(p => p.Stock),
            ("stock", "desc") => query.OrderByDescending(p => p.Stock),
            _                 => query.OrderBy(p => p.Name)
        };

        return await query.ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id) =>
        await _db.Products.FindAsync(id);

    public async Task<Product> CreateAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> UpdateAsync(Guid id, Product updated)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return null;

        product.Name        = updated.Name;
        product.Description = updated.Description;
        product.Price       = updated.Price;
        product.Stock       = updated.Stock;
        product.ImageUrl    = updated.ImageUrl;
        product.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return false;

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return true;
    }
}