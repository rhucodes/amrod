using AmrodAssessment.Models;
using AmrodAssessment.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AmrodAssessment.Controllers.Api;

[ApiController]
[Route("api/payfast")]
public class PayFastController : ControllerBase
{
    private readonly IPayFastService _payFastService;
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly ILogger<PayFastController> _logger;

    public PayFastController(
        IPayFastService payFastService,
        IOrderService orderService,
        ICustomerService customerService,
        ILogger<PayFastController> logger)
    {
        _payFastService = payFastService;
        _orderService = orderService;
        _customerService = customerService;
        _logger = logger;
    }

    // Called by our frontend to get the PayFast payment URL + form data
    [HttpGet("initiate/{orderId:guid}")]
    public async Task<IActionResult> Initiate(Guid orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        if (order is null)
            return NotFound(new { message = "Order not found." });

        var customer = await _customerService.GetByIdAsync(order.CustomerId);
        if (customer is null)
            return NotFound(new { message = "Customer not found." });

        var data = _payFastService.BuildPaymentData(
            order,
            customer.Email,
            customer.FirstName,
            customer.LastName);

        var pfConfig = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetSection("PayFast");

        return Ok(new
        {
            paymentUrl = pfConfig["BaseUrl"],
            formData = data
        });
    }

    // ITN webhook PayFast calls this after payment
    [HttpPost("notify")]
    public async Task<IActionResult> Notify([FromForm] IFormCollection form)
    {
        _logger.LogInformation("PayFast ITN received");

        foreach (var kvp in form)
            _logger.LogInformation("ITN field: {Key} = {Value}", kvp.Key, kvp.Value);

        // Preserve exact order and include ALL fields including empty ones
        var data = new Dictionary<string, string>();
        foreach (var kvp in form)
        {
            if (kvp.Key != "signature")
                data[kvp.Key] = kvp.Value.ToString();
        }

        var receivedSignature = form["signature"].ToString();

        if (!_payFastService.ValidateSignature(data, receivedSignature))
        {
            _logger.LogWarning("PayFast ITN signature validation failed");
            return BadRequest("Invalid signature");
        }

        var paymentStatus = data.GetValueOrDefault("payment_status", "");
        var mPaymentId = data.GetValueOrDefault("m_payment_id", "");

        if (!Guid.TryParse(mPaymentId, out var orderId))
        {
            _logger.LogWarning("PayFast ITN invalid order id: {Id}", mPaymentId);
            return BadRequest("Invalid order id");
        }

        _logger.LogInformation("PayFast ITN payment_status: {Status} for order: {Id}", paymentStatus, orderId);

        var newStatus = paymentStatus switch
        {
            "COMPLETE" => OrderStatus.Confirmed,
            "CANCELLED" => OrderStatus.Cancelled,
            _ => OrderStatus.Pending
        };

        await _orderService.UpdateStatusAsync(orderId, newStatus);

        return Ok();
    }

    [HttpGet("debug/{orderId:guid}")]
    public async Task<IActionResult> Debug(Guid orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        if (order is null) return NotFound();

        var customer = await _customerService.GetByIdAsync(order.CustomerId);
        if (customer is null) return NotFound();

        var pf = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetSection("PayFast");
        var passPhrase = pf["PassPhrase"];

        var data = new Dictionary<string, string>
        {
            ["merchant_id"] = pf["MerchantId"]!,
            ["merchant_key"] = pf["MerchantKey"]!,
            ["return_url"] = pf["ReturnUrl"]!,
            ["cancel_url"] = pf["CancelUrl"]!,
            ["notify_url"] = pf["NotifyUrl"]!,
            ["name_first"] = customer.FirstName,
            ["name_last"] = customer.LastName,
            ["email_address"] = customer.Email,
            ["m_payment_id"] = order.Id.ToString(),
            ["amount"] = order.TotalAmount.ToString("F2"),
            ["item_name"] = $"Amrod Order {order.Id.ToString()[..8].ToUpper()}",
        };

        // Build the raw string before hashing
        var sb = new StringBuilder();
        foreach (var kvp in data)
            sb.Append($"{kvp.Key}={_payFastService.UrlEncodePublic(kvp.Value)}&");

        var rawStringWithoutPassphrase = sb.ToString().TrimEnd('&');
        var rawStringWithPassphrase = sb + $"passphrase={_payFastService.UrlEncodePublic(passPhrase!)}";

        return Ok(new
        {
            rawStringWithoutPassphrase,
            rawStringWithPassphrase,
            generatedSignature = _payFastService.BuildSignature(data),
            fields = data
        });
    }
}