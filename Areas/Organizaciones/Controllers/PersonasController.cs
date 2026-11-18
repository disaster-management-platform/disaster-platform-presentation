using disaster_platform_web.Areas.Personas.Clients;
using disaster_platform_web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Areas.Personas.Controllers;

[Area("Personas")]
public class PersonasController(IPersonasClient api) : PantallaController(api)
{
    [HttpGet("Personas")]
    public Task<IActionResult> Peo01() => Ver("PEO-01", null);

    [HttpGet("Personas/Lista")]
    public Task<IActionResult> Peo02() => Ver("PEO-02", null);

    [HttpGet("Personas/Crear")]
    public Task<IActionResult> Peo03() => Ver("PEO-03", null);

    [HttpPost("Personas/Crear"), ValidateAntiForgeryToken]
    public Task<IActionResult> Peo03Guardar([Bind(Prefix = "Values")] Dictionary<string, string?> values) => Guardar("PEO-03", null, values);

    [HttpGet("Personas/{id}")]
    public Task<IActionResult> Peo04(string id) => Ver("PEO-04", id);

    [HttpPost("Personas/{id}/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Peo04Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("PEO-04", id, dialog, values);

    [HttpGet("Personas/Familias")]
    public Task<IActionResult> Peo05() => Ver("PEO-05", null);

    [HttpPost("Personas/Familias/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Peo05Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("PEO-05", null, dialog, values);

    [HttpGet("Personas/Familias/{id}")]
    public Task<IActionResult> Peo06(string id) => Ver("PEO-06", id);

    [HttpPost("Personas/Familias/{id}/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Peo06Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("PEO-06", id, dialog, values);
}
