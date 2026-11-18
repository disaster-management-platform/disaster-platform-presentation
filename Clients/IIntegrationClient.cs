public interface IIntegrationClient
{
    Task<IReadOnlyList<T>> ListarAsync<T>(string recurso, int? disasterId, CancellationToken ct = default);
}
public class IntegrationClient(HttpClient http) : IIntegrationClient
{
    public async Task<IReadOnlyList<T>> ListarAsync<T>(string recurso, int? disasterId, CancellationToken ct)
    {
        var url = $"api/eden/{recurso}" + (disasterId is { } d ? $"?disasterId={d}" : "");
        return await http.GetFromJsonAsync<List<T>>(url, ct) ?? [];
    }
}