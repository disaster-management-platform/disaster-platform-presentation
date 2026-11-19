using System.Text.Json;
using System.Text.Json.Nodes;
using disaster_platform_web.Clients;
using disaster_platform_web.Screens;
namespace disaster_platform_web.Areas.Danos.Clients;
public interface IDanosClient : IPantallaClient { Task<string> DownloadAsync(string id,string evidenceId,CancellationToken ct); }
public class FakeDanosClient : FakePantallaClient, IDanosClient { public Task<string> DownloadAsync(string id,string evidenceId,CancellationToken ct)=>throw new NotSupportedException(); }
public class DanosClient(HttpClient http) : HttpPantallaClient(http), IDanosClient
{
    SelectOption[] assets=[],categories=[];
    public override async Task PrepararAsync(PantallaVm vm,IReadOnlyDictionary<string,string?> query,CancellationToken ct)
    {
        await base.PrepararAsync(vm,query,ct);
        var catalogs=await Get("api/evaluaciones/catalogos",ct);
        assets=Options(catalogs?["assetTypes"]);categories=Rows(catalogs?["damageCategories"]).Where(x=>Text(x,"active")!="false").Select(x=>new SelectOption(Text(x,"id"),Text(x,"name")+" — "+Text(x,"assetTypeId")+" / "+Text(x,"damageLevel"))).ToArray();
        vm.Screen=vm.Screen with{Api="Evaluaciones — contratos Jakarta EE",State='A'};
        if(vm.Screen.Id=="DAM-04")Fields(InputFields().Where(x=>x.Name!="expectedVersion").Concat([F("evidence","Evidencias JPEG, PNG o PDF (6 MiB)",FT.File)]).ToArray());
        if(vm.Screen.Id=="DAM-03")vm.Screen=vm.Screen with{Dialogs=[new("editar","Editar Draft","Editar Draft",InputFields()),new("rev","Nueva revisión","Nueva revisión",InputFields()),new("aprobar","Validar","Validar evaluación",[F("reason","Justificación",FT.TextArea,true)]),new("rechazar","Rechazar","Rechazar evaluación",[F("reason","Justificación",FT.TextArea,true)]),new("evidencia","Adjuntar evidencia","Adjuntar y asociar evidencia",[F("evidence","JPEG, PNG o PDF (6 MiB)",FT.File,true)])]};
        if(vm.Screen.Id=="DAM-05")Disable("Los pendientes offline se consultan en Android. No existe un endpoint de conflictos Web.");
        if(vm.Screen.Id=="DAM-06")vm.Screen=vm.Screen with{Dialogs=[]};
        if(vm.Screen.Id=="DAM-07")Disable("La priorización se consulta en las propuestas de Reconstrucción; no existe un contrato agregado de impacto.");
    }
    Field[] InputFields()=>[Select("disasterId","Desastre (vacío = preliminar)",View.Disasters),F("assetId","Identificador estable del activo",required:true),Select("assetTypeId","Tipo de activo",assets,true),Select("damageCategoryId","Categoría de daño",categories,true),F("evaluatedAt","Evaluado el",FT.DateTime,true),F("location","Ubicación",required:true),F("latitude","Latitud",FT.Number),F("longitude","Longitud",FT.Number),F("description","Descripción",FT.TextArea,true),F("budget","Presupuesto conocido (COP)",FT.Number,true)];
    protected override async Task<JsonNode?> Read(Screen s,string? id,int sub,CancellationToken ct)
    {
        if(s.Id=="DAM-03") {
            if(sub==0)return await Get($"api/evaluaciones/{id}/evidencias",ct);
            if(sub==1) {
                var history=Rows(await Get($"api/evaluaciones/{id}/historial",ct));
                return new JsonArray(history.Select(entry=>{var snapshot=JsonNode.Parse(Text(entry,"snapshot"))!;snapshot["action"]=entry["action"]?.DeepClone();snapshot["actor"]=entry["actor"]?.DeepClone();snapshot["recordedAt"]=entry["recordedAt"]?.DeepClone();return snapshot;}).ToArray());
            }
            var detail=await Get("api/evaluaciones/"+id,ct);return detail;
        }
        if(View.Unsupported!=null)return new JsonArray();
        var result=await Get("api/evaluaciones"+DisasterQuery(),ct);var rows=Rows(result);
        if(s.Id=="DAM-06")return new JsonArray(rows.Where(x=>Text(x,"status")=="Draft").Select(x=>x.DeepClone()).ToArray());
        View.RealStats=true;View.Screen=View.Screen with{Stats=[new("Evaluaciones",rows.Length.ToString()),new("Draft",rows.Count(x=>Text(x,"status")=="Draft").ToString()),new("Validadas",rows.Count(x=>Text(x,"status")=="Validated").ToString())]};
        View.PointsJson=System.Text.Json.JsonSerializer.Serialize(rows.Where(x=>x["latitude"]!=null&&x["longitude"]!=null).Select(x=>new{lat=x["latitude"],lng=x["longitude"],name=Text(x,"assetId"),sev=1,polygon=x["geometry"]}));return result;
    }
    public async Task<string> DownloadAsync(string id,string evidenceId,CancellationToken ct)
    {
        var download=await Get($"api/evaluaciones/{id}/evidencias/{evidenceId}/descarga",ct);
        var url=Text(download,"url");
        if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="https")throw new ContractException("URL de descarga inválida.");
        return url;
    }
    protected override IReadOnlyList<AttachmentLink> DownloadLinks(Screen s,JsonNode item,string? parent,int sub) => s.Id=="DAM-03"&&sub==0&&Text(item,"status")=="Verified" ? [new($"/Danos/{parent}/Evidencias/{Text(item,"evidenceId")}/Descarga",Text(item,"originalFileName"))] : [];
    protected override string Cell(JsonNode x,string key)=>key switch{"evidence"=>Text(x,"originalFileName","reference"),"disaster"=>Text(x,"disasterId"),_=>base.Cell(x,key)};
    record EvaluationInput(int? DisasterId,string AssetId,string AssetTypeId,string DamageCategoryId,DateTimeOffset EvaluatedAt,string Location,double? Latitude,double? Longitude,JsonNode? Geometry,string Description,string[] Evidence,decimal Budget,long? ExpectedVersion);
    record Transition(string Status,long ExpectedVersion,string Reason);
    EvaluationInput Input(Dictionary<string,string?> v,JsonNode? old=null)=>new(Integer(v,"disasterId"),Required(v,"assetId"),Required(v,"assetTypeId"),Required(v,"damageCategoryId"),Date(v,"evaluatedAt")??throw new ContractException("Falta fecha"),Required(v,"location"),Number(v,"latitude"),Number(v,"longitude"),old?["geometry"]?.DeepClone(),Required(v,"description"),Rows(old?["evidence"]).Select(x=>x.ToString()).ToArray(),Money(v,"budget"),old==null?null:Version(v));
    public override async Task<string?> EnviarAsync(Screen s,string? dialog,Dictionary<string,string?> v,Guid key,string? id,CancellationToken ct)
    {
        JsonNode? result;
        if(s.Id=="DAM-04") {
            result=await Send(HttpMethod.Post,"api/evaluaciones",Input(v),key,ct);id=Text(result,"evaluationId");
            if(string.IsNullOrEmpty(id))throw new ContractException("La creación no confirmó evaluationId.");
        }else if(dialog is "aprobar" or "rechazar")result=await Send(HttpMethod.Put,$"api/evaluaciones/{id}/estado",new Transition(dialog=="aprobar"?"Validated":"Rejected",Version(v),Required(v,"reason")),key,ct);
        else if(dialog is "editar" or "rev") {
            var old=await Get("api/evaluaciones/"+id,ct);
            result=await Send(dialog=="editar"?HttpMethod.Put:HttpMethod.Post,$"api/evaluaciones/{id}"+(dialog=="rev"?"/revisiones":""),Input(v,old),key,ct);
        }else if(dialog=="evidencia")result=await Get("api/evaluaciones/"+id,ct);
        else throw Unsupported();
        if(View.Files.Count>0) {
            var refs=new List<string>();
            foreach(var file in View.Files) {
                if(file.Length>6*1024*1024)throw new ContractException("La evidencia supera 6 MiB.");
                using var stream=file.OpenReadStream();
                var hash=await System.Security.Cryptography.SHA256.HashDataAsync(stream,ct);
                var uploadKey=new Guid(System.Security.Cryptography.SHA256.HashData(key.ToByteArray().Concat(hash).ToArray()).AsSpan(0,16));
                var metadata=await Upload($"api/evaluaciones/{id}/evidencias",file,"",uploadKey,true,ct);
                if(Text(metadata,"status")!="Verified")throw new ContractException("La evidencia no está verificada. Conserva el archivo para reintentar.");
                refs.Add("attachment:"+Text(metadata,"evidenceId"));
            }
            var current=await Get("api/evaluaciones/"+id,ct)??throw new ContractException("Evaluación no disponible.");
            if(Text(current,"status")!="Draft")throw new ContractException("La evaluación cambió de estado; conserva las evidencias para revisión.");
            var existing=Rows(current["evidence"]).Select(x=>x.ToString()).ToArray();
            if(refs.Except(existing).Any()) {
                if(Text(current,"revision")!=Text(result,"revision") || Text(current,"version")!=Text(result,"version")) throw new HttpRequestException("La evaluación cambió durante la subida; revisa las evidencias verificadas.",null,System.Net.HttpStatusCode.Conflict);
                var data=current.Deserialize<EvaluationInput>(Json)!;
                result=await Send(HttpMethod.Put,"api/evaluaciones/"+id,data with{ExpectedVersion=current["version"]!.GetValue<long>(),Evidence=existing.Concat(refs).Distinct().ToArray()},key,ct);
            }
        }
        return id??Text(result,"evaluationId");
    }
}
