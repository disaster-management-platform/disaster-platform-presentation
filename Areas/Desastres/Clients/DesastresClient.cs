using System.Net;
using System.Net.Http.Json;
namespace disaster_platform_web.Areas.Desastres.Clients;

public interface IDesastresClient
{
    Task<IReadOnlyList<DesastreDto>> ListarAsync(CancellationToken ct = default);
    Task<DesastreDto?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<DesastreDto> CrearAsync(DesastreInput i, CancellationToken ct = default);
}

public class DesastresClient(HttpClient http) : IDesastresClient
{
    public async Task<IReadOnlyList<DesastreDto>> ListarAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<DesastreDto>>("api/desastres", ct) ?? [];

    public async Task<DesastreDto?> ObtenerAsync(int id, CancellationToken ct)
    {
        var r = await http.GetAsync($"api/desastres/{id}", ct);
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<DesastreDto>(ct);
    }

    public async Task<DesastreDto> CrearAsync(DesastreInput i, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/desastres") { Content = JsonContent.Create(i) };
        req.Headers.Add("Idempotency-Key", i.Key.ToString());
        var r = await http.SendAsync(req, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<DesastreDto>(ct))!;
    }
}

public class FakeDesastresClient : IDesastresClient
{
    static readonly List<DesastreDto> D = new()
    {
        new(1, "Sismo Armenia", "Armenia, Quindío", "Sismo", 4, 4.5339, -75.6811, null, "Response", 1),
        new(2, "Inundación Cali", "Cali, Valle", "Inundación", 3, 3.4516, -76.5320, 5000, "Registered", 1),
        new(3, "Deslizamiento Manizales", "Manizales, Caldas", "Deslizamiento", 3, 5.0703, -75.5138, null, "Recovery", 1),
        new(4, "Incendio Quibdó", "Quibdó, Chocó", "Incendio", 2, 5.6947, -76.6583, null, "Closed", 1),
    };
    public Task<IReadOnlyList<DesastreDto>> ListarAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<DesastreDto>>(D);
    public Task<DesastreDto?> ObtenerAsync(int id, CancellationToken ct) => Task.FromResult<DesastreDto?>(D.FirstOrDefault(x => x.Id == id) ?? D[0]);
    public Task<DesastreDto> CrearAsync(DesastreInput i, CancellationToken ct) => Task.FromResult(D[0]);
}