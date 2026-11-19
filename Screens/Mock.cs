using System.Globalization;
using System.Text.Json;

namespace disaster_platform_web.Screens;

// Datos de ejemplo con la forma de los DTO. Se reemplaza por HttpClient cuando exista el backend.
public static class Mock
{
    static readonly CultureInfo Co = new("es-CO");

    public static readonly (double Lat, double Lng, string Name, int Sev)[] Points =
    {
        (4.5339, -75.6811, "Sismo Armenia", 4),
        (3.4516, -76.5320, "Inundación Cali", 3),
        (5.0703, -75.5138, "Deslizamiento Manizales", 3),
        (5.6947, -76.6583, "Incendio Quibdó", 2),
    };

    public static readonly (string Zone, int Count)[] Bars =
    {
        ("Armenia", 420), ("Cali", 310), ("Manizales", 260), ("Quibdó", 150),
    };

    public static (string Zone, int Count)[] BarsFor(Screen screen) => screen.Id switch
    {
        "RES-01" =>
        [
            ("Agua", 112), ("Alimentos", 86), ("Medicamentos", 58), ("Vehículos", 34),
            ("Maquinaria", 22), ("Equipos de rescate", 19), ("Personal", 151), ("Refugios", 4),
        ],
        "DAM-07" =>
        [
            ("Armenia", 6300), ("Calarcá", 4200), ("Cali", 5100), ("Manizales", 2800),
        ],
        _ => Bars
    };

    public static string PointsJson(bool single) =>
        JsonSerializer.Serialize((single ? Points.Take(1) : Points)
            .Select(p => new { lat = p.Lat, lng = p.Lng, name = p.Name, sev = p.Sev }));

    public static string Cell(Col c, int i)
    {
        if (c.Sample is { Length: > 0 } s) return s[i % s.Length];
        return c.Type switch
        {
            CT.Date => DateTime.Now.AddHours(-(i * 7 + 3)).ToString("yyyy-MM-dd HH:mm"),
            CT.Money => (1_500_000m * (i % 5 + 1)).ToString("C0", Co),
            CT.Number => (i * 3 + 2).ToString(),
            _ => $"{c.Label} {i + 1}"
        };
    }

    public static IEnumerable<Row> Rows(Col[] cols, string? rowTo, int n = 8)
    {
        for (var i = 0; i < n; i++)
            yield return new Row(
                rowTo is null ? null : Catalog.RowUrl(rowTo, i),
                cols.Select(c => (Cell(c, i), c.Type == CT.Badge)).ToArray());
    }

    static readonly string[] Danger = { "destroy", "crít", "crit", "grave", "severe", "rech", "reject", "fall", "error", "conflict", "alta", "high", "4", "5" };
    static readonly string[] Success = { "valid", "cerr", "complet", "closed", "activo", "entreg", "applied", "consolid", "formaliz", "selected", "sí" };
    static readonly string[] Warning = { "moder", "pend", "draft", "media", "en curso", "progress", "propos", "abiert", "3" };

    public static string Badge(string v)
    {
        var t = v.ToLowerInvariant();
        var kind = Danger.Any(t.Contains) ? "danger"
                 : Success.Any(t.Contains) ? "success"
                 : Warning.Any(t.Contains) ? "warning" : "secondary";
        return $"badge text-bg-{kind}";
    }
}