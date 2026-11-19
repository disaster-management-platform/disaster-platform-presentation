using disaster_platform_web.Areas.Reconstruccion.Clients;
using disaster_platform_web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Areas.Reconstruccion.Controllers;

[Area("Reconstruccion")]
public class ReconstruccionController(IReconstruccionClient api) : PantallaController(api)
{
    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Adjuntos/{attachmentId:guid}")]
    public async Task<IActionResult> Descargar(string id,string attachmentId)
    {
        using var response=await api.DownloadAsync(id,attachmentId,HttpContext.RequestAborted);
        using var content=await response.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
        using var data=new MemoryStream();var buffer=new byte[81920];int count;
        while((count=await content.ReadAsync(buffer,HttpContext.RequestAborted))>0){if(data.Length+count>20*1024*1024)return StatusCode(502);await data.WriteAsync(buffer.AsMemory(0,count),HttpContext.RequestAborted);}
        return File(data.ToArray(),response.Content.Headers.ContentType?.MediaType??"application/octet-stream",Path.GetFileName(response.Content.Headers.ContentDisposition?.FileNameStar??response.Content.Headers.ContentDisposition?.FileName?.Trim('"')??attachmentId));
    }

    [HttpGet("Reconstruccion")]
    public Task<IActionResult> Rec01() => Ver("REC-01", null);

    [HttpGet("Reconstruccion/Propuestas")]
    public Task<IActionResult> Rec02() => Ver("REC-02", null);

    [HttpPost("Reconstruccion/Propuestas/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec02Accion(string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-02", null, dialog, values);

    [HttpGet("Reconstruccion/Propuestas/{id:guid}")]
    public Task<IActionResult> Rec03(string id) => Ver("REC-03", id);

    [HttpPost("Reconstruccion/Propuestas/{id:guid}/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec03Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-03", id, dialog, values);

    [HttpGet("Reconstruccion/Proyectos")]
    public Task<IActionResult> Rec04() => Ver("REC-04", null);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}")]
    public Task<IActionResult> Rec05(string id) => Ver("REC-05", id);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Formalizacion")]
    public Task<IActionResult> Rec06(string id) => Ver("REC-06", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Formalizacion"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec06Guardar(string id, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Guardar("REC-06", id, values);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Formalizacion/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec06Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-06", id, dialog, values);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Contrato")]
    public Task<IActionResult> Rec07(string id) => Ver("REC-07", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Contrato"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec07Guardar(string id, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Guardar("REC-07", id, values);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Cronograma")]
    public Task<IActionResult> Rec08(string id) => Ver("REC-08", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Cronograma/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec08Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-08", id, dialog, values);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Tareas")]
    public Task<IActionResult> Rec09(string id) => Ver("REC-09", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Tareas/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec09Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-09", id, dialog, values);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Participantes")]
    public Task<IActionResult> Rec10(string id) => Ver("REC-10", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Participantes/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec10Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-10", id, dialog, values);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Finanzas")]
    public Task<IActionResult> Rec11(string id) => Ver("REC-11", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Finanzas/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec11Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-11", id, dialog, values);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Reportes")]
    public Task<IActionResult> Rec12(string id) => Ver("REC-12", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Reportes"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec12Guardar(string id, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Guardar("REC-12", id, values);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Cierre")]
    public Task<IActionResult> Rec13(string id) => Ver("REC-13", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Cierre/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec13Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-13", id, dialog, values);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Entidades")]
    public Task<IActionResult> Rec14(string id) => Ver("REC-14", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Entidades/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec14Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-14", id, dialog, values);

    [HttpGet("Reconstruccion/Proyectos/{id:guid}/Seguimiento")]
    public Task<IActionResult> Rec15(string id) => Ver("REC-15", id);

    [HttpPost("Reconstruccion/Proyectos/{id:guid}/Seguimiento/Accion/{dialog}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Rec15Accion(string id, string dialog, [Bind(Prefix = "Values")] Dictionary<string, string?> values) => Accion("REC-15", id, dialog, values);
}
