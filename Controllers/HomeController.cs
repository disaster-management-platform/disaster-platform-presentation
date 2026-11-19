using disaster_platform_web.Clients;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Controllers;

public class HomeController(IPantallaClient api) : PantallaController(api)
{
    [HttpGet("/")] public Task<IActionResult> Index() => Ver("HOME");
}
