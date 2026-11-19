using disaster_platform_web.Screens;

namespace disaster_platform_web.Clients;

// Contrato común de los clientes por área. Cada área tiene su interfaz, su Fake y su HttpClient.
public interface IPantallaClient
{
    bool Simulado { get; }
    Task<IReadOnlyList<Row>?> FilasAsync(Screen s, Col[] cols, string? rowTo, string? id, int sub, CancellationToken ct);
    Task<IReadOnlyDictionary<string, string>?> DetalleAsync(Screen s, string? id, CancellationToken ct);
    Task<string?> EnviarAsync(Screen s, string? dialog, Dictionary<string, string?> valores, Guid key, string? id, CancellationToken ct);
    Task PrepararAsync(PantallaVm vm, IReadOnlyDictionary<string, string?> query, CancellationToken ct);
}

// Vista previa exclusiva de Development (Api:UseMock = true).
public class FakePantallaClient : IPantallaClient
{
    public Task PrepararAsync(PantallaVm vm, IReadOnlyDictionary<string, string?> query, CancellationToken ct) => Task.CompletedTask;
    public bool Simulado => true;
    public virtual Task<IReadOnlyList<Row>?> FilasAsync(Screen s, Col[] cols, string? rowTo, string? id, int sub, CancellationToken ct)
    {
        var count = s.Id == "REC-13" || (s.Id == "REC-05" && (sub == 0 || sub == 1)) ? 5 : 8;
        return Task.FromResult<IReadOnlyList<Row>?>(Mock.Rows(cols, rowTo, count).ToList());
    }
    public virtual Task<IReadOnlyDictionary<string, string>?> DetalleAsync(Screen s, string? id, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<string, string>?>(null);
    public Task<string?> EnviarAsync(Screen s, string? dialog, Dictionary<string, string?> valores, Guid key, string? id, CancellationToken ct) =>
        Task.FromResult<string?>(null);
}
