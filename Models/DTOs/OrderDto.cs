namespace AmrodAssessment.Models.DTOs;

public class ProductDto
{
    public Guid    Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string  Description { get; set; } = string.Empty;
    public decimal Price       { get; set; }
    public int     Stock       { get; set; }
    public string  ImageUrl    { get; set; } = string.Empty;
}

public class CustomerDto
{
    public Guid   Id        { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public string Phone     { get; set; } = string.Empty;
}

public class OrderItemDto
{
    public Guid       Id        { get; set; }
    public int        Quantity  { get; set; }
    public decimal    UnitPrice { get; set; }
    public ProductDto Product   { get; set; } = null!;
}

public class OrderDto
{
    public Guid           Id          { get; set; }
    public OrderStatus    Status      { get; set; }
    public decimal        TotalAmount { get; set; }
    public string         Notes       { get; set; } = string.Empty;
    public DateTime       CreatedAt   { get; set; }
    public CustomerDto    Customer    { get; set; } = null!;
    public List<OrderItemDto> Items   { get; set; } = new();
}