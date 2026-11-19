using disaster_platform_web.Areas.Recursos.Clients;
using disaster_platform_web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Areas.Recursos.Controllers;

[Area("Recursos")]
public class RecursosController(IRecursosClient api) : PantallaController(api)
{
    [HttpGet("Recursos")]
    public Task<IActionResult> Res01() => Ver("RES-01", null);

    [HttpGet("Recursos/Inventario")]
    public Task<IActionResult> Res02() => Ver("RES-02", null);

    [HttpPost("Recursos/Inventario/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Res02Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("RES-02", null, dialog, values);

    [HttpGet("Recursos/Inventario/{id:guid}")]
    public Task<IActionResult> Res03(string id) => Ver("RES-03", id);

    [HttpPost("Recursos/Inventario/{id:guid}/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Res03Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("RES-03", id, dialog, values);

    [HttpGet("Recursos/Solicitudes/Crear")]
    public Task<IActionResult> Res04() => Ver("RES-04", null);

    [HttpPost("Recursos/Solicitudes/Crear"), ValidateAntiForgeryToken]
    public Task<IActionResult> Res04Guardar([Bind(Prefix = "Values")] Dictionary<string, string?> values) => Guardar("RES-04", null, values);

    [HttpGet("Recursos/Solicitudes")]
    public Task<IActionResult> Res05() => Ver("RES-05", null);

    [HttpPost("Recursos/Solicitudes/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Res05Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("RES-05", null, dialog, values);

    [HttpGet("Recursos/Donaciones")]
    public Task<IActionResult> Res06() => Ver("RES-06", null);

    [HttpPost("Recursos/Donaciones/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Res06Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("RES-06", null, dialog, values);
}
