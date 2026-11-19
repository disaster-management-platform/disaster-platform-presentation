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

    [HttpGet("Administracion/Activos")]
    public Task<IActionResult> Adm02() => Ver("ADM-02", null);

    [HttpPost("Administracion/Activos/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Adm02Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("ADM-02", null, dialog, values);

    [HttpGet("Administracion/Usuarios")]
    public Task<IActionResult> Adm03() => Ver("ADM-03", null);

    [HttpPost("Administracion/Usuarios/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Adm03Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("ADM-03", null, dialog, values);
}
