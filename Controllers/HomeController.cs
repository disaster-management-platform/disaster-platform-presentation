using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")] public IActionResult Index() => Redirect("/Desastres");
}
