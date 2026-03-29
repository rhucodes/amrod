using AmrodAssessment.Models;
using AmrodAssessment.Models.DTOs;
using AmrodAssessment.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AmrodAssessment.Controllers.Api;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(orders.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order is null) return NotFound(new { message = "Order not found." });
        return Ok(ToDto(order));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var items = request.Items
            .Select(i => new OrderRequestItem(i.ProductId, i.Quantity))
            .ToList();

        var (order, error) = await _orderService.CreateAsync(request.CustomerId, items, request.Notes);

        if (error is not null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = order!.Id }, ToDto(order));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var order = await _orderService.UpdateStatusAsync(id, request.Status);
        if (order is null) return NotFound(new { message = "Order not found." });
        return Ok(ToDto(order));
    }

    private static OrderDto ToDto(Order order) => new()
    {
        Id = order.Id,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        Notes = order.Notes,
        CreatedAt = order.CreatedAt,
        Customer = new CustomerDto
        {
            Id = order.Customer.Id,
            FirstName = order.Customer.FirstName,
            LastName = order.Customer.LastName,
            Email = order.Customer.Email,
            Phone = order.Customer.Phone
        },
        Items = order.OrderItems.Select(oi => new OrderItemDto
        {
            Id = oi.Id,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            Product = new ProductDto
            {
                Id = oi.Product.Id,
                Name = oi.Product.Name,
                Description = oi.Product.Description,
                Price = oi.Product.Price,
                Stock = oi.Product.Stock,
                ImageUrl = oi.Product.ImageUrl
            }
        }).ToList()
    };
}

public record OrderItemRequest(
    [Required] Guid ProductId,
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")] int Quantity
);

public record OrderRequest(
    [Required] Guid CustomerId,
    [Required][MinLength(1, ErrorMessage = "Order must contain at least one item.")] List<OrderItemRequest> Items,
    string? Notes
);

public record UpdateStatusRequest(
    [Required] OrderStatus Status
);