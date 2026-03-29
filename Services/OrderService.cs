using AmrodAssessment.Data;
using AmrodAssessment.Models;
using Microsoft.EntityFrameworkCore;

namespace AmrodAssessment.Services;

public interface IOrderService
{
    Task<IEnumerable<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(Guid id);
    Task<(Order? order, string? error)> CreateAsync(Guid customerId, List<OrderRequestItem> items, string? notes);
    Task<Order?> UpdateStatusAsync(Guid id, OrderStatus status);
}

public record OrderRequestItem(Guid ProductId, int Quantity);

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Order>> GetAllAsync() =>
        await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<Order?> GetByIdAsync(Guid id) =>
        await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<(Order? order, string? error)> CreateAsync(
        Guid customerId,
        List<OrderRequestItem> items,
        string? notes)
    {
        var customer = await _db.Customers.FindAsync(customerId);
        if (customer is null)
            return (null, "Customer not found.");

        // Load all products in one query
        var productIds = items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        // Validate all products exist and have sufficient stock
        foreach (var item in items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is null)
                return (null, $"Product {item.ProductId} not found.");
            if (product.Stock < item.Quantity)
                return (null, $"Insufficient stock for '{product.Name}'. Available: {product.Stock}.");
        }

        // Build order
        var orderItems = items.Select(item =>
        {
            var product = products.First(p => p.Id == item.ProductId);
            return new OrderItem
            {
                ProductId = item.ProductId,
                Quantity  = item.Quantity,
                UnitPrice = product.Price
            };
        }).ToList();

        var order = new Order
        {
            CustomerId   = customerId,
            Notes        = notes ?? string.Empty,
            Status       = OrderStatus.Pending,
            TotalAmount  = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
            OrderItems   = orderItems
        };

        // Decrement stock
        foreach (var item in items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            product.Stock     -= item.Quantity;
            product.UpdatedAt  = DateTime.UtcNow;
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return (order, null);
    }

    public async Task<Order?> UpdateStatusAsync(Guid id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return null;

        order.Status    = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return order;
    }
}