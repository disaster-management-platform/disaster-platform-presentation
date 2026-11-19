using disaster_platform_web.Areas.Sistema.Clients;
using disaster_platform_web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Areas.Sistema.Controllers;

[Area("Sistema")]
public class AuditoriaController(ISistemaClient api) : PantallaController(api)
{
    [HttpGet("Auditoria")]
    public Task<IActionResult> Aud01() => Ver("AUD-01", null);
}
