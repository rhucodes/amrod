using Microsoft.AspNetCore.Mvc;

namespace AmrodAssessment.Controllers;

public class CartController : Controller
{
    public IActionResult Index() => View();
}