using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AmrodAssessment.Models;

namespace AmrodAssessment.Services;

public interface IPayFastService
{
    Dictionary<string, string> BuildPaymentData(Order order, string customerEmail, string firstName, string lastName);
    string BuildSignature(Dictionary<string, string> data);
    bool ValidateSignature(Dictionary<string, string> data, string receivedSignature);
    string UrlEncodePublic(string value);
}

public class PayFastService : IPayFastService
{
    private readonly IConfiguration _config;
    private readonly ILogger<PayFastService> _logger;

    public PayFastService(IConfiguration config, ILogger<PayFastService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string UrlEncodePublic(string value) => UrlEncode(value);

    public Dictionary<string, string> BuildPaymentData(Order order, string customerEmail, string firstName, string lastName)
    {
        var pf = _config.GetSection("PayFast");

        var data = new Dictionary<string, string>
        {
            ["merchant_id"] = pf["MerchantId"]!,
            ["merchant_key"] = pf["MerchantKey"]!,
            ["return_url"] = pf["ReturnUrl"]!,
            ["cancel_url"] = pf["CancelUrl"]!,
            ["notify_url"] = pf["NotifyUrl"]!,
            ["name_first"] = firstName,
            ["name_last"] = lastName,
            ["email_address"] = customerEmail,
            ["m_payment_id"] = order.Id.ToString(),
            ["amount"] = order.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["item_name"]     = $"Amrod Order {order.Id.ToString()[..8].ToUpper()}",
        };

        data["signature"] = BuildSignature(data);
        return data;
    }

    public string BuildSignature(Dictionary<string, string> data)
    {
        var passPhrase = _config.GetSection("PayFast")["PassPhrase"];

        var sb = new StringBuilder();
        foreach (var kvp in data.Where(kvp => kvp.Key != "signature" && !string.IsNullOrEmpty(kvp.Value)))
            sb.Append($"{kvp.Key}={UrlEncode(kvp.Value)}&");

        if (!string.IsNullOrWhiteSpace(passPhrase))
            sb.Append($"passphrase={UrlEncode(passPhrase)}");
        else
            sb.Length--;

        var md5        = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(sb.ToString());
        var hash       = md5.ComputeHash(inputBytes);

        var result = new StringBuilder();
        foreach (var b in hash)
            result.Append(b.ToString("x2"));

        return result.ToString();
    }

public bool ValidateSignature(Dictionary<string, string> data, string receivedSignature)
{
    var passPhrase = _config.GetSection("PayFast")["PassPhrase"];

    var sb         = new StringBuilder();
    var filtered   = data
        .Where(kvp => kvp.Key != "signature")
        .Where(kvp => kvp.Key != "billing_date" || !string.IsNullOrEmpty(kvp.Value))
        .Where(kvp => kvp.Key != "token"        || !string.IsNullOrEmpty(kvp.Value))
        .ToList();

    var last = filtered.LastOrDefault();

    foreach (var kvp in filtered)
    {
        var isLast = kvp.Key == last.Key;

        if (string.IsNullOrWhiteSpace(passPhrase) && isLast)
            sb.Append($"{kvp.Key}={UrlEncode(kvp.Value)}");
        else
            sb.Append($"{kvp.Key}={UrlEncode(kvp.Value)}&");
    }

    if (!string.IsNullOrWhiteSpace(passPhrase))
        sb.Append($"passphrase={UrlEncode(passPhrase)}");

    _logger.LogInformation("ITN raw string: {Raw}", sb.ToString());

    var md5        = MD5.Create();
    var inputBytes = Encoding.ASCII.GetBytes(sb.ToString());
    var hash       = md5.ComputeHash(inputBytes);

    var result = new StringBuilder();
    foreach (var b in hash)
        result.Append(b.ToString("x2"));

    var expectedSignature = result.ToString();

    _logger.LogInformation("Expected: {Expected} Received: {Received}", expectedSignature, receivedSignature);

    return string.Equals(expectedSignature, receivedSignature, StringComparison.OrdinalIgnoreCase);
}

    private static string UrlEncode(string url)
    {
        var convertPairs = new Dictionary<string, string>
        {
            { "%",  "%25" }, { "!",  "%21" }, { "#",  "%23" }, { " ",  "+"   },
            { "$",  "%24" }, { "&",  "%26" }, { "'",  "%27" }, { "(",  "%28" },
            { ")",  "%29" }, { "*",  "%2A" }, { "+",  "%2B" }, { ",",  "%2C" },
            { "/",  "%2F" }, { ":",  "%3A" }, { ";",  "%3B" }, { "=",  "%3D" },
            { "?",  "%3F" }, { "@",  "%40" }, { "[",  "%5B" }, { "]",  "%5D" }
        };

        var replaceRegex = new Regex(@"[%!# $&'()*+,/:;=?@\[\]]");
        return replaceRegex.Replace(url, match => convertPairs[match.Value]);
    }
}