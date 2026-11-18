[Area("Desastres")]
public class DesastresController(IDesastresClient api) : Controller
{
    [HttpGet("Desastres")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await api.ListarAsync(ct));

    [HttpGet("Desastres/Lista")]
    public async Task<IActionResult> Lista(CancellationToken ct)
    {
        var s = Catalog.Match("/Desastres/Lista")!.Value.Item1;   // columnas y filtros de DIS-02
        var rows = (await api.ListarAsync(ct)).Select(d => new Row($"/Desastres/{d.Id}",
            s.Cols.Select(c => (Valor(d, c), c.Type == CT.Badge)).ToArray()));
        return View(new DesastreListaVm(new TableVm(s.TableTitle, s.Cols, s.RowTo, s.Filters) { Rows = rows }));
    }

    static string Valor(DesastreDto d, Col c) => c.Key.ToLowerInvariant() switch
    {
        "nombre" => d.Nombre,
        "ubicacion" or "ubicación" => d.Ubicacion,
        "severidad" => d.Severidad.ToString(),
        "phase" or "fase" => d.Phase,
        _ => ""
    };

    [HttpGet("Desastres/{id:int}")]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct) =>
        await api.ObtenerAsync(id, ct) is { } d ? View(d) : NotFound();

    [HttpGet("Desastres/Crear")]
    public IActionResult Crear() => View(new DesastreInput());

    [HttpPost("Desastres/Crear"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(DesastreInput i, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(i);
        try { var d = await api.CrearAsync(i, ct); return Redirect($"/Desastres/{d.Id}"); }
        catch (HttpRequestException e) when ((int?)e.StatusCode is 409 or 422)
        {
            ModelState.AddModelError("", e.StatusCode == HttpStatusCode.Conflict
                ? "El registro cambió, recarga y revisa." : "La operación no cumple una regla de negocio.");
            return View(i);
        }
    }
}