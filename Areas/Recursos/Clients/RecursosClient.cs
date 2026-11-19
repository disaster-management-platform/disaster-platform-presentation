using disaster_platform_web.Clients;

namespace disaster_platform_web.Areas.Recursos.Clients;

public interface IRecursosClient : IIntegrationClient { }

public class FakeRecursosClient : FakePantallaClient, IRecursosClient { }

public class RecursosClient(HttpClient http) : HttpPantallaClient(http), IRecursosClient
{
    public override async Task PrepararAsync(disaster_platform_web.Screens.PantallaVm vm,IReadOnlyDictionary<string,string?> query,CancellationToken ct) { await base.PrepararAsync(vm,query,ct); Disable("Utiliza las interfaces nativas autorizadas de Eden o el sistema propietario; esta acción no tiene contrato Web disponible."); }
    protected override Task<System.Text.Json.Nodes.JsonNode?> Read(disaster_platform_web.Screens.Screen s,string? id,int sub,CancellationToken ct)=>Task.FromResult<System.Text.Json.Nodes.JsonNode?>(new System.Text.Json.Nodes.JsonArray());
    public override Task<string?> EnviarAsync(disaster_platform_web.Screens.Screen s,string? dialog,Dictionary<string,string?> v,Guid key,string? id,CancellationToken ct)=>throw Unsupported();
}
