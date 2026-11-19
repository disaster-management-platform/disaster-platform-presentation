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
        var s = Catalog.Get(sid);
        vm ??= new PantallaVm();
        vm.Screen = s;
        vm.RouteId = id;
        vm.Parent = Catalog.ParentOf(Request.Path.Value ?? "/");
        vm.Flash = TempData["Msg"] as string;
        vm.FlashError ??= TempData["Err"] as string;
        var ct = HttpContext.RequestAborted;
        try
        {
            if (s.Kind == Kind.List || (s.Kind == Kind.Dashboard && s.Cols.Length > 0))
                vm.Filas = await api.FilasAsync(s, s.Cols, s.RowTo, id, -1, ct);
            for (var i = 0; i < s.Tables.Length; i++)
                vm.FilasSub[i] = await api.FilasAsync(s, s.Tables[i].Cols, s.Tables[i].RowTo, id, i, ct) ?? [];
            if (s.Kind == Kind.Detail)
                vm.Detalle = await api.DetalleAsync(s, id, ct);
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
            var status = vm.Detalle?.GetValueOrDefault("status") ?? "Monitoring";
            vm.ProjectStatus = status;
            vm.Screen = s with { Stage = Etapa(status) };
        }
        else if (s.Stage == 3 && api.Simulado && vm.ProjectStatus is null)
            vm.ProjectStatus = "Monitoring";
        return View("Pantalla", vm);
    }

    static int Etapa(string status) => status.ToLowerInvariant() switch
    {
        "draft" => 2,
        "formalized" or "monitoring" => 3,
        "closed" => 4,
        _ => 3
    };

    protected async Task<IActionResult> Guardar(string sid, string? id, Dictionary<string, string?> values)
    {
        var s = Catalog.Get(sid);
        var vm = new PantallaVm { Values = values, Key = Clave() };
        foreach (var f in s.Fields) Validar(vm.Errors, values, f);
        if (vm.Errors.Count > 0) return await Ver(sid, id, vm);
        try
        {
            await api.EnviarAsync(s, null, values, vm.Key, id, HttpContext.RequestAborted);
            TempData["Msg"] = api.Simulado ? "Guardado (datos de ejemplo, Api:UseMock=true)." : "Guardado.";
            return Redirect(s.SaveTo is null ? Request.Path.Value! : Catalog.Url(s.SaveTo, id));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            vm.FlashError = Mensaje(e);
            return await Ver(sid, id, vm);
        }
    }

    protected async Task<IActionResult> Accion(string sid, string? id, string dialog, Dictionary<string, string?> values)
    {
        var s = Catalog.Get(sid);
        var d = s.Dialogs.FirstOrDefault(x => x.Id == dialog);
        if (d is null) return NotFound();
        var errores = new Dictionary<string, string>();
        foreach (var f in d.Fields) Validar(errores, values, f);
        if (errores.Count > 0)
            TempData["Err"] = $"{d.Title}: " + string.Join(" ", errores.Select(e => $"{e.Key} {e.Value}"));
        else
            try
            {
                await api.EnviarAsync(s, dialog, values, Clave(), id, HttpContext.RequestAborted);
                TempData["Msg"] = api.Simulado ? $"{d.Title}: acción simulada (Api:UseMock=true)." : $"{d.Title}: listo.";
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                TempData["Err"] = $"{d.Title}: {Mensaje(e)}";
            }
        return Redirect(Catalog.Url(sid, id));
    }

    Guid Clave() => Guid.TryParse(Request.Form["Key"], out var k) ? k : Guid.NewGuid();

    static string Mensaje(Exception e) => (e as HttpRequestException)?.StatusCode switch
    {
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => "La operación no cumple una regla de negocio.",
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
