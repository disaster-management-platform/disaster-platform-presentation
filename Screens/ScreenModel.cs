using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace disaster_platform_web.Screens;

// PageModel único. Resuelve la pantalla del catálogo según la URL y atiende formularios y diálogos.
public class ScreenModel : PageModel
{
    public Screen Screen { get; private set; } = default!;
    public string? RouteId { get; private set; }
    public Screen? Parent { get; private set; }

    [BindProperty] public Dictionary<string, string?> Values { get; set; } = new();
    public Dictionary<string, string> Errors { get; } = new();

    public string? Flash => TempData["Msg"] as string;
    public string? FlashError => TempData["Err"] as string;

    bool Resolve()
    {
        var m = Catalog.Match(Request.Path.Value ?? "/");
        if (m is null) return false;
        (Screen, RouteId) = m.Value;
        Parent = Catalog.ParentOf(Request.Path.Value ?? "/");
        return true;
    }

    public IActionResult OnGet() => Resolve() ? Page() : NotFound();

    // Guardar formulario principal (sin backend: valida y redirige)
    public IActionResult OnPost()
    {
        if (!Resolve()) return NotFound();
        foreach (var f in Screen.Fields) Check(f);
        if (Errors.Count > 0) return Page();

        TempData["Msg"] = "Guardado (simulado, todavía no hay backend).";
        var to = Screen.SaveTo is null ? Request.Path.Value! : Catalog.Url(Screen.SaveTo, RouteId);
        return Redirect(to);
    }

    // Diálogos de las pantallas de detalle y listado
    public IActionResult OnPostAction(string dialog)
    {
        if (!Resolve()) return NotFound();
        var d = Screen.Dialogs.FirstOrDefault(x => x.Id == dialog);
        if (d is null) return NotFound();

        foreach (var f in d.Fields) Check(f);
        if (Errors.Count == 0) TempData["Msg"] = $"{d.Title}: acción simulada, todavía no hay backend.";
        else TempData["Err"] = $"{d.Title}: " + string.Join(" ", Errors.Select(e => $"{e.Key} {e.Value}"));
        return Redirect(Request.Path.Value ?? "/");
    }

    void Check(Field f)
    {
        if (f.Type is FT.Bool or FT.File) return;
        Values.TryGetValue(f.Name, out var raw);
        var v = raw?.Trim();

        if (string.IsNullOrEmpty(v))
        {
            if (f.Required) Errors[f.Name] = "es obligatorio.";
            return;
        }
        if (f.Max > 0 && v.Length > f.Max) Errors[f.Name] = $"admite máximo {f.Max} caracteres.";

        switch (f.Type)
        {
            case FT.Number:
                if (!double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) Errors[f.Name] = "debe ser un número.";
                else if (f.Min is { } lo && d < lo) Errors[f.Name] = $"debe ser mayor o igual a {lo}.";
                else if (f.Maximum is { } hi && d > hi) Errors[f.Name] = $"debe ser menor o igual a {hi}.";
                break;
            case FT.Date or FT.DateTime:
                if (!DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) Errors[f.Name] = "fecha inválida.";
                else if (f.NoFuture && dt > DateTime.Now) Errors[f.Name] = "no puede ser futura.";
                break;
            case FT.Email:
                if (!new EmailAddressAttribute().IsValid(v)) Errors[f.Name] = "correo inválido.";
                break;
        }
    }
}