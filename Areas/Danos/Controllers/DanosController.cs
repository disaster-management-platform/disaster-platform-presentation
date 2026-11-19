using disaster_platform_web.Areas.Danos.Clients;
using disaster_platform_web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Areas.Danos.Controllers;

[Area("Danos")]
public class DanosController(IDanosClient api) : PantallaController(api)
{
    [HttpGet("Danos")]
    public Task<IActionResult> Dam01() => Ver("DAM-01", null);

    [HttpGet("Danos/Lista")]
    public Task<IActionResult> Dam02() => Ver("DAM-02", null);

    [HttpGet("Danos/{id:guid}")]
    public Task<IActionResult> Dam03(string id) => Ver("DAM-03", id);

    [HttpPost("Danos/{id:guid}/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dam03Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("DAM-03", id, dialog, values);

    [HttpGet("Danos/Crear")]
    public Task<IActionResult> Dam04() => Ver("DAM-04", null);

    [HttpPost("Danos/Crear"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dam04Guardar([Bind(Prefix = "Values")] Dictionary<string, string?> values) => Guardar("DAM-04", null, values);

    [HttpGet("Danos/Sincronizacion")]
    public Task<IActionResult> Dam05() => Ver("DAM-05", null);

    [HttpPost("Danos/Sincronizacion/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dam05Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("DAM-05", null, dialog, values);

    [HttpGet("Danos/Validacion")]
    public Task<IActionResult> Dam06() => Ver("DAM-06", null);

    [HttpPost("Danos/Validacion/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dam06Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("DAM-06", null, dialog, values);

    [HttpGet("Danos/Priorizacion")]
    public Task<IActionResult> Dam07() => Ver("DAM-07", null);
}
