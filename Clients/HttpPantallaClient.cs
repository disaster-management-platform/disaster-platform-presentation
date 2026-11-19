using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using disaster_platform_web.Screens;

namespace disaster_platform_web.Clients;

// Cliente HTTP genérico. Las rutas salen de Screen.Api (columna "contrato" de la plantilla).
// Mientras el contrato esté en estado N/E son una propuesta, se ajustan en Screen.Api, no en el código.
public abstract class HttpPantallaClient(HttpClient http) : IPantallaClient
{
    static readonly Regex Verbo = new(@"((?:GET|POST|PUT|PATCH)(?:/(?:GET|POST|PUT|PATCH))*)\s+(/[^\s;]+)", RegexOptions.IgnoreCase);
    public bool Simulado => false;

    static string? Ruta(Screen s, string verbo, string? id)
    {
        foreach (Match m in Verbo.Matches(s.Api))
        {
            if (!m.Groups[1].Value.Contains(verbo, StringComparison.OrdinalIgnoreCase)) continue;
            var p = m.Groups[2].Value;
            p = p.Split('?')[0].Replace("{id}", id ?? "");
            return p.TrimStart('/');
        }
        return null;
    }

    public async Task<IReadOnlyList<Row>?> FilasAsync(Screen s, Col[] cols, string? rowTo, string? id, int sub, CancellationToken ct)
    {
        if (sub >= 0) return [];   // subtablas: se habilitan cuando el contrato publique su endpoint
        var ruta = Ruta(s, "GET", id);
        if (ruta is null) return [];
        var items = await http.GetFromJsonAsync<List<Dictionary<string, JsonElement>>>(ruta, ct) ?? [];
        return items.Select(it => new Row(
            rowTo is null || !it.TryGetValue("id", out var rid) ? null : Catalog.Url(rowTo, rid.ToString()),
            cols.Select(c => (Texto(it, c.Key), c.Type == CT.Badge)).ToArray())).ToList();
    }

    public async Task<IReadOnlyDictionary<string, string>?> DetalleAsync(Screen s, string? id, CancellationToken ct)
    {
        var ruta = Ruta(s, "GET", id);
        if (ruta is null) return null;
        var r = await http.GetAsync(ruta, ct);
        r.EnsureSuccessStatusCode();
        var d = await r.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(ct);
        return d?.ToDictionary(k => k.Key, k => k.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task EnviarAsync(Screen s, string? dialog, IDictionary<string, string?> valores, Guid key, string? id, CancellationToken ct)
    {
        var verbo = dialog is null ? "POST" : "PUT";
        var ruta = Ruta(s, verbo, id) ?? Ruta(s, "POST", id) ?? Ruta(s, "PATCH", id) ?? Ruta(s, "GET", id);
        if (ruta is null) throw new HttpRequestException("Sin endpoint de escritura", null, System.Net.HttpStatusCode.NotImplemented);
        var req = new HttpRequestMessage(verbo == "PUT" && Ruta(s, "PUT", id) is not null ? HttpMethod.Put : HttpMethod.Post, ruta)
        { Content = JsonContent.Create(valores) };
        req.Headers.Add("Idempotency-Key", key.ToString());
        (await http.SendAsync(req, ct)).EnsureSuccessStatusCode();
    }

    static string Texto(Dictionary<string, JsonElement> it, string key)
    {
        var k = it.Keys.FirstOrDefault(x => x.Equals(key, StringComparison.OrdinalIgnoreCase));
        return k is null ? "" : it[k].ToString();
    }
}
