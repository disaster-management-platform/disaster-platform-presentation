using disaster_platform_web.Areas.Desastres.Clients;
using disaster_platform_web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Areas.Desastres.Controllers;

[Area("Desastres")]
public class DesastresController(IDesastresClient api) : PantallaController(api)
{
    [HttpGet("Desastres")]
    public Task<IActionResult> Dis01() => Ver("DIS-01", null);

    [HttpGet("Desastres/Lista")]
    public Task<IActionResult> Dis02() => Ver("DIS-02", null);

    [HttpGet("Desastres/{id:int}")]
    public Task<IActionResult> Dis03(string id) => Ver("DIS-03", id);

    [HttpPost("Desastres/{id:int}/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dis03Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("DIS-03", id, dialog, values);

    [HttpGet("Desastres/Crear")]
    public Task<IActionResult> Dis04() => Ver("DIS-04", null);

    [HttpPost("Desastres/Crear"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dis04Guardar([Bind(Prefix = "Values")] Dictionary<string, string?> values) => Guardar("DIS-04", null, values);

    [HttpGet("Desastres/{id:int}/Fuentes")]
    public Task<IActionResult> Dis05(string id) => Ver("DIS-05", id);

    [HttpPost("Desastres/{id:int}/Fuentes/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dis05Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("DIS-05", id, dialog, values);

    [HttpGet("Desastres/{id:int}/Organismos")]
    public Task<IActionResult> Dis06(string id) => Ver("DIS-06", id);

    [HttpPost("Desastres/{id:int}/Organismos/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dis06Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("DIS-06", id, dialog, values);

    [HttpGet("Desastres/{id:int}/Alertas")]
    public Task<IActionResult> Dis07(string id) => Ver("DIS-07", id);

    [HttpPost("Desastres/{id:int}/Alertas/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dis07Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("DIS-07", id, dialog, values);

    [HttpGet("Desastres/{id:int}/Evolucion")]
    public Task<IActionResult> Dis08(string id) => Ver("DIS-08", id);

    [HttpPost("Desastres/{id:int}/Evolucion/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Dis08Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("DIS-08", id, dialog, values);
}
