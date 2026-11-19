using disaster_platform_web.Areas.Sistema.Clients;
using disaster_platform_web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Areas.Sistema.Controllers;

[Area("Sistema")]
public class IntegracionesController(ISistemaClient api) : PantallaController(api)
{
    [HttpGet("Integraciones")]
    public Task<IActionResult> Int01() => Ver("INT-01", null);

    [HttpPost("Integraciones/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Int01Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("INT-01", null, dialog, values);
}
