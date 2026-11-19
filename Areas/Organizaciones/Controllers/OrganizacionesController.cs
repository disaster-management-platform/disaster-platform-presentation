using disaster_platform_web.Areas.Organizaciones.Clients;
using disaster_platform_web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Areas.Organizaciones.Controllers;

[Area("Organizaciones")]
public class OrganizacionesController(IOrganizacionesClient api) : PantallaController(api)
{
    [HttpGet("Organizaciones")]
    public Task<IActionResult> Org01() => Ver("ORG-01", null);

    [HttpGet("Organizaciones/Crear")]
    public Task<IActionResult> Org03() => Ver("ORG-03", null);

    [HttpPost("Organizaciones/Crear"), ValidateAntiForgeryToken]
    public Task<IActionResult> Org03Guardar([Bind(Prefix = "Values")] Dictionary<string, string?> values) =>
        Guardar("ORG-03", null, values);

    [HttpGet("Organizaciones/Tareas")]
    public Task<IActionResult> Org04() => Ver("ORG-04", null);

    [HttpPost("Organizaciones/Tareas/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Org04Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) =>
        Accion("ORG-04", null, dialog, values);

    [HttpGet("Organizaciones/Actividades")]
    public Task<IActionResult> Org05() => Ver("ORG-05", null);

    [HttpPost("Organizaciones/Actividades/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Org05Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) =>
        Accion("ORG-05", null, dialog, values);

    [HttpGet("Organizaciones/Alertas")]
    public Task<IActionResult> Org06() => Ver("ORG-06", null);

    [HttpPost("Organizaciones/Alertas/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Org06Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) =>
        Accion("ORG-06", null, dialog, values);

    [HttpGet("Organizaciones/Bitacora")]
    public Task<IActionResult> Org07() => Ver("ORG-07", null);

    [HttpGet("Organizaciones/Tareas/{id}")]
    public Task<IActionResult> Org08(string id) => Ver("ORG-08", id);

    [HttpPost("Organizaciones/Tareas/{id}/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Org08Accion(
        string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) =>
        Accion("ORG-08", id, dialog, values);

    [HttpGet("Organizaciones/{id}")]
    public Task<IActionResult> Org02(string id) => Ver("ORG-02", id);

    [HttpPost("Organizaciones/{id}/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Org02Accion(
        string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) =>
        Accion("ORG-02", id, dialog, values);

}
