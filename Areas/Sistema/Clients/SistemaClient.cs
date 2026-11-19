using disaster_platform_web.Clients;

namespace disaster_platform_web.Areas.Sistema.Clients;

public interface ISistemaClient : IPantallaClient { }

public class FakeSistemaClient : FakePantallaClient, ISistemaClient { }

public class SistemaClient(HttpClient http) : HttpPantallaClient(http), ISistemaClient
{
    public override async Task PrepararAsync(disaster_platform_web.Screens.PantallaVm vm,IReadOnlyDictionary<string,string?> query,CancellationToken ct) { await base.PrepararAsync(vm,query,ct); if(vm.Screen.Id=="HOME") { var rows=Rows(await Get("api/desastres",ct)); vm.RealStats=true; vm.Screen=vm.Screen with{Cols=[],Stats=[new("Desastres",rows.Length.ToString()),new("Activos",rows.Count(x=>Text(x,"phase")!="Closed").ToString())],Map=false}; return; } Disable("Utiliza las interfaces nativas autorizadas de Eden o el sistema propietario; esta acción no tiene contrato Web disponible."); }
    protected override Task<System.Text.Json.Nodes.JsonNode?> Read(disaster_platform_web.Screens.Screen s,string? id,int sub,CancellationToken ct)=>Task.FromResult<System.Text.Json.Nodes.JsonNode?>(new System.Text.Json.Nodes.JsonArray());
    public override Task<string?> EnviarAsync(disaster_platform_web.Screens.Screen s,string? dialog,Dictionary<string,string?> v,Guid key,string? id,CancellationToken ct)=>throw Unsupported();
}
