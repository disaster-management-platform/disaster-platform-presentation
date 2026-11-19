using disaster_platform_web.Clients;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Controllers;

public class CuentaController(IPantallaClient api) : PantallaController(api)
{
    [HttpGet("Cuenta/Acceder")] public Task<IActionResult> Acceder() => Ver("AUTH");

    // Pendiente: SignOut de OIDC (Keycloak). Por ahora vuelve al acceso.
    [HttpPost("Cuenta/Salir"), ValidateAntiForgeryToken]
    public IActionResult Salir() => Redirect("/Cuenta/Acceder");
}
