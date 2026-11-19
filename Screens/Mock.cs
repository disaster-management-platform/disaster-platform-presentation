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

    public static readonly (string Zone, decimal Count)[] Bars =
    {
        ("Armenia", 420), ("Cali", 310), ("Manizales", 260), ("Quibdó", 150),
    };

    public static (string Zone, decimal Count)[] BarsFor(Screen screen) => screen.Id switch
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

    public static string ChartTypeFor(string screenId)
    {
        var seed = 0;
        for (var i = 0; i < screenId.Length; i++)
            seed += (i + 1) * screenId[i];

        return (seed % 3) switch
        {
            0 => "bars",
            1 => "columns",
            _ => "pie"
        };
    }

    static readonly string[] ChartCategoryKeys =
    [
        "ubicacion", "location", "zona", "zone", "zonas", "damageLevel", "damage", "eventType",
        "status", "phase", "severidad", "disaster", "tipo", "assetType", "channel", "subject",
    ];

    static int ChartCategoryIndex(Col[] cols)
    {
        foreach (var key in ChartCategoryKeys)
        {
            var index = Array.FindIndex(cols, col => col.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) return index;
        }
        return Array.FindIndex(cols, col => col.Type is CT.Text or CT.Badge);
    }

    public static int ChartTableIndex(SubTable[] tables)
    {
        for (var i = 0; i < tables.Length; i++)
            if (tables[i].Cols.Any(col => ChartCategoryKeys.Contains(col.Key, StringComparer.OrdinalIgnoreCase))) return i;
        return tables.Length > 0 ? 0 : -1;
    }

    public static (string Zone, decimal Count)[] ChartBars(Col[] cols, IReadOnlyList<Row>? rows, bool mapFallback = false)
    {
        if (rows is null || rows.Count == 0) return mapFallback ? BarsFromPoints() : [];

        var categoryIndex = ChartCategoryIndex(cols);
        if (categoryIndex < 0) return mapFallback ? BarsFromPoints() : [];

        var metricIndex = Array.FindIndex(cols, col =>
            col.Type is CT.Number or CT.Money
            && (col.Key.Contains("actual", StringComparison.OrdinalIgnoreCase)
                || col.Key.Contains("gasto", StringComparison.OrdinalIgnoreCase)
                || col.Key.Contains("spent", StringComparison.OrdinalIgnoreCase)));
        if (metricIndex < 0)
            metricIndex = Array.FindIndex(cols, col => col.Type is CT.Number or CT.Money);
        var entries = rows
            .Where(row => categoryIndex < row.Cells.Length && !string.IsNullOrWhiteSpace(row.Cells[categoryIndex].Text))
            .Select(row =>
            {
                decimal? value = metricIndex >= 0 && metricIndex < row.Cells.Length
                    ? ParseChartValue(cols[metricIndex], row.Cells[metricIndex].Text)
                    : null;
                return (Category: row.Cells[categoryIndex].Text, Value: value);
            })
            .ToArray();
        if (entries.Length == 0) return mapFallback ? BarsFromPoints() : [];

        var grouped = entries.GroupBy(entry => entry.Category, StringComparer.OrdinalIgnoreCase);
        if (metricIndex >= 0 && entries.All(entry => entry.Value.HasValue))
            return grouped.Select(group => (group.Key, Math.Max(0m, group.Sum(entry => entry.Value!.Value))))
                .OrderByDescending(bar => bar.Item2).ToArray();

        return grouped.Select(group => (group.Key, (decimal)group.Count()))
            .OrderByDescending(bar => bar.Item2).ToArray();
    }

    static decimal? ParseChartValue(Col col, string text)
    {
        var style = col.Type == CT.Money ? NumberStyles.Currency : NumberStyles.Number;
        return decimal.TryParse(text, style, Co, out var value) ? value : null;
    }

    public static string ChartTitleFor(Col[] cols, string? tableTitle)
    {
        var categoryIndex = ChartCategoryIndex(cols);
        if (categoryIndex < 0) return "Distribución de registros";

        var measure = cols.FirstOrDefault(col => col.Type is CT.Number or CT.Money)?.Label;
        var subject = measure ?? tableTitle?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Registros";
        return $"{subject} por {cols[categoryIndex].Label.ToLowerInvariant()}";
    }

    static (string Zone, decimal Count)[] BarsFromPoints() =>
        Points.GroupBy(point => point.Sev)
            .OrderByDescending(group => group.Key)
            .Select(group => ($"Severidad {group.Key}", (decimal)group.Count()))
            .ToArray();

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
        var priority = cols.FirstOrDefault(c => c.Key == "priority");
        for (var i = 0; i < n; i++)
        {
            var cells = cols.Select(c =>
            {
                var text = c.Key == "resultado" && priority is not null
                    ? Resultado(Cell(priority, i))
                    : Cell(c, i);
                return (text, c.Type == CT.Badge);
            }).ToArray();
            yield return new Row(
                rowTo is null ? null : Catalog.RowUrl(rowTo, i),
                cells,
                new Guid(i + 1, 0, 0, new byte[8]).ToString());
        }
    }

    public static string Resultado(string? priority) => priority?.ToLowerInvariant() switch
    {
        "high" => "Se prioriza proyecto",
        "low" => "Se propone proyecto",
        "unassigned" => "Se propone proyecto (presupuesto no asignado)",
        _ => ""
    };

    static readonly string[] Danger = { "no disponible", "en corte", "agotad", "lleno", "destroy", "crít", "crit", "grave", "severe", "rech", "reject", "fall", "error", "conflict", "bloquead", "alta", "high", "4", "5" };
    static readonly string[] Success = { "valid", "cerr", "complet", "closed", "activo", "entreg", "applied", "consolid", "formaliz", "selected", "disponible", "atendid", "recibid", "distribu", "aprob", "exitos", "normal", "acept", "sí" };
    static readonly string[] Warning = { "moder", "pend", "draft", "media", "en curso", "ejecuc", "progress", "propos", "abiert", "degrad", "verificaci", "tránsito", "parcial", "prueba", "reintent", "suspend", "3" };

    public static string Badge(string v)
    {
        var t = v.ToLowerInvariant();
        var kind = Danger.Any(t.Contains) ? "danger"
                 : Warning.Any(t.Contains) ? "warning"
                 : Success.Any(t.Contains) && !t.Contains("inactivo", StringComparison.Ordinal) ? "success" : "secondary";
        return $"badge text-bg-{kind}";
    }
}