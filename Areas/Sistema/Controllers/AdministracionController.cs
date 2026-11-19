using disaster_platform_web.Areas.Sistema.Clients;
using disaster_platform_web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Areas.Sistema.Controllers;

[Area("Sistema")]
public class AdministracionController(ISistemaClient api) : PantallaController(api)
{
    [HttpGet("Administracion")]
    public Task<IActionResult> Adm01() => Ver("ADM-01", null);

    [HttpPost("Administracion/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Adm01Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("ADM-01", null, dialog, values);
}
