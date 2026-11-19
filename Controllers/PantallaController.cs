using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using disaster_platform_web.Clients;
using disaster_platform_web.Screens;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Controllers;

// Lógica común de los controllers: arma el ViewModel con el cliente del área, valida y envía.
public abstract class PantallaController(IPantallaClient api) : Controller
{
    protected async Task<IActionResult> Ver(string sid, string? id = null, PantallaVm? vm = null)
    {
        ViewData["EdenUrl"] = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiOptions>>().Value.EdenUrl;
        var s = Catalog.Get(sid);
        vm ??= new PantallaVm();
        vm.Screen = s;
        vm.Simulado = api.Simulado;
        if (!api.Simulado)
        {
            vm.Filas ??= [];
            for (var index = 0; index < s.Tables.Length; index++) vm.FilasSub.TryAdd(index, []);
        }
        vm.RouteId = id;
        vm.Parent = Catalog.ParentOf(Request.Path.Value ?? "/");
        vm.Flash = TempData["Msg"] as string;
        vm.FlashError ??= TempData["Err"] as string;
        var ct = HttpContext.RequestAborted;
        try
        {
            await api.PrepararAsync(vm, Request.Query.ToDictionary(x => x.Key, x => (string?)x.Value.ToString()), ct);
            s = vm.Screen;
            if (s.Kind == Kind.List || (s.Kind == Kind.Dashboard && s.Cols.Length > 0))
                vm.Filas = await api.FilasAsync(s, s.Cols, s.RowTo, id, -1, ct);
            if (s.Id == "REC-02" && vm.Filas is not null)
                vm.Filas = vm.Filas
                    .OrderBy(row => Celda(row, s, "zone"), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(row => Celda(row, s, "assetType"), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(row => RangoDanio(Celda(row, s, "damage")))
                    .ThenBy(row => Celda(row, s, "damage"), StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            for (var i = 0; i < s.Tables.Length; i++)
                vm.FilasSub[i] = await api.FilasAsync(s, s.Tables[i].Cols, s.Tables[i].RowTo, id, i, ct) ?? [];
            if (id is not null && s.Id != "ORG-08" || s.Kind == Kind.Detail)
            {
                vm.Detalle = await api.DetalleAsync(s, id, ct);
                if (vm.Detalle is not null)
                    foreach (var (name, value) in vm.Detalle) vm.Values.TryAdd(name, value);
                if (vm.Detalle?.GetValueOrDefault("version") is { } version) vm.Values.TryAdd("expectedVersion", version);
            }
            if (s.Stage == 3 && id is not null)
            {
                var proyecto = await api.DetalleAsync(Catalog.Get("REC-05"), id, ct);
                vm.ProjectStatus = proyecto?.GetValueOrDefault("status");
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {   // degradación controlada: la pantalla se muestra vacía con el aviso
            vm.FlashError = Mensaje(e);
            vm.Filas ??= [];
            for (var i = 0; i < s.Tables.Length; i++) vm.FilasSub.TryAdd(i, []);
        }
        if (s.Id == "REC-05")
        {
            var status = vm.Detalle?.GetValueOrDefault("status") ?? "";
            vm.ProjectStatus = status;
            vm.Screen = s with { Stage = Etapa(status) };
        }
        else if (s.Stage == 3 && api.Simulado && vm.ProjectStatus is null)
            vm.ProjectStatus = "Monitoring";
        if (s.Id == "REC-13")
        {
            var canFinalize = vm.Detalle?.GetValueOrDefault("canFinalize");
            var sampleCanFinalize = Mock.Cell(s.Cols.Single(c => c.Key == "canFinalize"), 0);
            vm.CanFinalize = ParseFlag(canFinalize) ?? (api.Simulado && ParseFlag(sampleCanFinalize) == true);
            var missing = vm.Detalle?.GetValueOrDefault("activitiesWithoutReports");
            if (int.TryParse(missing, out var missingCount))
                vm.ActivitiesWithoutReports = missingCount;
            else if (api.Simulado && int.TryParse(Mock.Cell(s.Cols.Single(c => c.Key == "activitiesWithoutReports"), 0), out missingCount))
                vm.ActivitiesWithoutReports = missingCount;
        }
        return View("Pantalla", vm);
    }

    static bool? ParseFlag(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "true" or "1" or "yes" or "sí" or "si" => true,
        "false" or "0" or "no" => false,
        _ => null
    };

    static int Etapa(string status) => status.ToLowerInvariant() switch
    {
        "draft" => 2,
        "formalized" or "monitoring" => 3,
        "closed" => 4,
        _ => 3
    };

    static string Celda(Row row, Screen screen, string key)
    {
        var index = Array.FindIndex(screen.Cols, column => column.Key == key);
        return index >= 0 && index < row.Cells.Length ? row.Cells[index].Text : "";
    }

    static int RangoDanio(string value) => value.ToLowerInvariant() switch
    {
        "none" or "sin daño" => 0,
        "minor" or "leve" => 1,
        "moderate" or "moderado" => 2,
        "severe" or "grave" => 3,
        "destroyed" or "destrucción total" => 4,
        _ => 5
    };

    protected async Task<IActionResult> Guardar(string sid, string? id, Dictionary<string, string?> values)
    {
        var s = Catalog.Get(sid);
        var vm = new PantallaVm { Screen = s, RouteId = id, Values = values, Key = Clave(), Files = Request.Form.Files.ToArray() };
        try { await api.PrepararAsync(vm, Request.Query.ToDictionary(x => x.Key, x => (string?)x.Value.ToString()), HttpContext.RequestAborted); s = vm.Screen; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { vm.FlashError = Mensaje(e); return await Ver(sid,id,vm); }
        foreach (var f in s.Fields)
        {
            if (f.Type == FT.File)
            {
                if (f.Required && !Request.Form.Files.Any(file =>
                    file.Name.Equals($"Values[{f.Name}]", StringComparison.OrdinalIgnoreCase) && file.Length > 0))
                    vm.Errors[f.Name] = "es obligatorio.";
            }
            else
                Validar(vm.Errors, values, f);
        }
        if (sid == "REC-06") ValidarCronograma(values, vm.Errors);
        if (vm.Errors.Count > 0) return await Ver(sid, id, vm);
        try
        {
            var resultId = await api.EnviarAsync(s, null, values, vm.Key, id, HttpContext.RequestAborted);
            if (!api.Simulado && id is null && s.SaveTo is not null && Catalog.Get(s.SaveTo).Route.Contains('{') && string.IsNullOrWhiteSpace(resultId)) throw new ContractException("El servicio no confirmó el identificador creado; reintenta con la misma operación.");
            TempData["Msg"] = api.Simulado ? "Guardado (datos de ejemplo, Api:UseMock=true)." : "Guardado.";
            return Redirect(s.SaveTo is null ? Request.Path.Value! : Catalog.Url(s.SaveTo, string.IsNullOrEmpty(resultId) ? id : resultId) + ContextQuery());
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            vm.FlashError = Mensaje(e);
            return await Ver(sid, id, vm);
        }
    }

    static void ValidarCronograma(Dictionary<string, string?> values, IDictionary<string, string> errors)
    {
        var lines = (values.GetValueOrDefault("activities") ?? "")
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            errors["activities"] = "Debe registrar al menos una actividad.";
            return;
        }

        decimal total = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var parts = lines[i].Split(';').Select(part => part.Trim()).ToArray();
            if (parts.Length != 5)
            {
                errors["activities"] = $"La actividad {i + 1} debe incluir tarea, responsable, fecha de inicio, vencimiento y presupuesto.";
                return;
            }
            if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                errors["activities"] = $"La actividad {i + 1} debe indicar una tarea y un responsable.";
                return;
            }
            if (!DateOnly.TryParseExact(parts[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startsAt)
                || !DateOnly.TryParseExact(parts[3], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueAt))
            {
                errors["activities"] = $"La actividad {i + 1} debe incluir fechas válidas.";
                return;
            }
            if (dueAt < startsAt)
            {
                errors["activities"] = $"La fecha de vencimiento de la actividad {i + 1} debe ser igual o posterior al inicio.";
                return;
            }
            if (!decimal.TryParse(parts[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var budget) || budget <= 0)
            {
                errors["activities"] = $"El presupuesto de la actividad {i + 1} debe ser mayor que cero.";
                return;
            }
            if (budget > decimal.MaxValue - total)
            {
                errors["activities"] = "La suma de los presupuestos de las actividades no es válida.";
                return;
            }
            total += budget;
        }

        if (decimal.TryParse(values.GetValueOrDefault("finalBudget"), NumberStyles.Number, CultureInfo.InvariantCulture, out var finalBudget)
            && total > finalBudget)
            errors["activities"] = "La suma de las actividades no puede superar el presupuesto final.";
    }

    protected async Task<IActionResult> Accion(string sid, string? id, string dialog, Dictionary<string, string?> values)
    {
        var s = Catalog.Get(sid);
        var actionVm = new PantallaVm { Screen = s, RouteId = id, Values = values, Key = Clave(), Files = Request.Form.Files.ToArray() };
        try { await api.PrepararAsync(actionVm, Request.Query.ToDictionary(x => x.Key, x => (string?)x.Value.ToString()), HttpContext.RequestAborted); s = actionVm.Screen; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { TempData["Err"] = Mensaje(e); return Redirect(Catalog.Url(sid,id)+ContextQuery()); }
        var d = s.Dialogs.FirstOrDefault(x => x.Id == dialog);
        if (d is null) return NotFound();
        if (sid == "REC-02" && dialog == "proyectos")
            return await CrearProyectos(s, id, values);
        if (sid == "REC-13" && dialog == "cerrar")
        {
            const string versionError = "No se pudo determinar la versión actual del proyecto.";
            if (!long.TryParse(values.GetValueOrDefault("expectedVersion"), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var expectedVersion) || expectedVersion < 0)
            {
                TempData["Err"] = versionError;
                return Redirect(Catalog.Url(sid, id)+ContextQuery());
            }
        }
        if (sid == "REC-13" && dialog == "cerrar")
        {
            try
            {
                var detail = await api.DetalleAsync(s, id, HttpContext.RequestAborted);
                var canFinalize = ParseFlag(detail?.GetValueOrDefault("canFinalize"));
                if (canFinalize is null && api.Simulado)
                    canFinalize = ParseFlag(Mock.Cell(s.Cols.Single(c => c.Key == "canFinalize"), 0));
                if (canFinalize != true)
                {
                    TempData["Err"] = "No se puede cerrar el proyecto mientras existan actividades incompletas o reportes pendientes.";
                    return Redirect(Catalog.Url(sid, id)+ContextQuery());
                }
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                TempData["Err"] = Mensaje(e);
                return Redirect(Catalog.Url(sid, id)+ContextQuery());
            }
        }
        var errores = new Dictionary<string, string>();
        foreach (var f in d.Fields) Validar(errores, values, f);
        if (errores.Count > 0)
            TempData["Err"] = $"{d.Title}: " + string.Join(" ", errores.Select(e => $"{e.Key} {e.Value}"));
        else
            try
            {
                var resultId = await api.EnviarAsync(s, dialog, values, actionVm.Key, id, HttpContext.RequestAborted);
                if (sid == "REC-03" && dialog == "proyecto" && !string.IsNullOrEmpty(resultId)) return Redirect(Catalog.Url("REC-05", resultId)+ContextQuery());
                TempData["Msg"] = api.Simulado ? $"{d.Title}: acción simulada (Api:UseMock=true)." : $"{d.Title}: listo." + (string.IsNullOrEmpty(resultId)?"":" ID: "+resultId);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                actionVm.FlashError = $"{d.Title}: {Mensaje(e)}";
                return await Ver(sid,id,actionVm);
            }
        return Redirect(Catalog.Url(sid, id)+ContextQuery());
    }

    async Task<IActionResult> CrearProyectos(Screen screen, string? id, Dictionary<string, string?> values)
    {
        var rawIds = values.GetValueOrDefault("ids") ?? "";
        var proposalIds = rawIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (proposalIds.Length == 0 || proposalIds.Any(proposalId => !Guid.TryParse(proposalId, out _)))
        {
            TempData["Err"] = "Selecciona al menos una propuesta válida.";
            return Redirect(Catalog.Url(screen.Id, id));
        }

        var creados = 0;
        try
        {
            foreach (var proposalId in proposalIds)
            {
                var requestValues = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase)
                {
                    ["ids"] = proposalId
                };
                var operationKey = new Guid(System.Security.Cryptography.SHA256.HashData(Clave().ToByteArray().Concat(Guid.Parse(proposalId).ToByteArray()).ToArray()).AsSpan(0,16));
                await api.EnviarAsync(screen, "proyectos", requestValues, operationKey, proposalId, HttpContext.RequestAborted);
                creados++;
            }
            TempData["Msg"] = $"{creados} proyectos creados.";
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            TempData["Err"] = creados == 0
                ? Mensaje(e)
                : $"Se crearon {creados} de {proposalIds.Length} proyectos. {Mensaje(e)}";
        }
        return Redirect(Catalog.Url(screen.Id, id));
    }

    string ContextQuery() => int.TryParse(Request.Query["disasterId"], out var id) && id > 0 ? "?disasterId=" + id : "";

    Guid Clave() => Guid.TryParse(Request.Form["Key"], out var k) ? k : Guid.NewGuid();

    static string Mensaje(Exception e) => (e as HttpRequestException)?.StatusCode switch
    {
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => e is ContractException ? e.Message : "La operación no cumple una regla de negocio.",
        HttpStatusCode.Unauthorized => "La sesión expiró, vuelve a ingresar.",
        HttpStatusCode.Forbidden => "No tienes permiso para esta operación.",
        HttpStatusCode.NotFound => "El recurso no existe.",
        HttpStatusCode.Conflict => "El registro cambió, recarga y revisa.",
        HttpStatusCode.TooManyRequests => "Demasiadas solicitudes, intenta en un momento.",
        HttpStatusCode.NotImplemented => "Esta operación aún no tiene contrato publicado.",
        _ => "El servicio no está disponible, intenta de nuevo."
    };

    static void Validar(IDictionary<string, string> errors, Dictionary<string, string?> values, Field f)
    {
        if (f.Type is FT.Bool or FT.File) return;
        values.TryGetValue(f.Name, out var raw);
        var v = raw?.Trim();
        if (string.IsNullOrEmpty(v)) { if (f.Required) errors[f.Name] = "es obligatorio."; return; }
        if (f.Max > 0 && v.Length > f.Max) errors[f.Name] = $"admite máximo {f.Max} caracteres.";
        switch (f.Type)
        {
            case FT.Number:
                if (!double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) errors[f.Name] = "debe ser un número.";
                else if (f.Min is { } lo && d < lo) errors[f.Name] = $"debe ser mayor o igual a {lo}.";
                else if (f.Maximum is { } hi && d > hi) errors[f.Name] = $"debe ser menor o igual a {hi}.";
                break;
            case FT.Date or FT.DateTime:
                if (!DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) errors[f.Name] = "fecha inválida.";
                else if (f.NoFuture && dt > DateTime.Now) errors[f.Name] = "no puede ser futura.";
                break;
            case FT.Email:
                if (!new EmailAddressAttribute().IsValid(v)) errors[f.Name] = "correo inválido.";
                break;
        }
    }
}
