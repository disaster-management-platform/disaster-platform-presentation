namespace disaster_platform_web.Controllers;

public class CuentaController : Controller
{
    [HttpGet("Cuenta/Acceder")]
    public IActionResult Acceder() => View();

    [HttpPost("Cuenta/Salir"), ValidateAntiForgeryToken]
    public IActionResult Salir() => Redirect("/Cuenta/Acceder");
}
