using System.Text.Json.Nodes;
using disaster_platform_web.Clients;
using disaster_platform_web.Screens;
namespace disaster_platform_web.Areas.Desastres.Clients;
public interface IDesastresClient : IPantallaClient { Task<System.Text.Json.Nodes.JsonNode?> SuggestionsAsync(int typeId,string location,DateTimeOffset startedAt,CancellationToken ct); }
public class FakeDesastresClient : FakePantallaClient, IDesastresClient { public Task<System.Text.Json.Nodes.JsonNode?> SuggestionsAsync(int typeId,string location,DateTimeOffset startedAt,CancellationToken ct) => throw new NotSupportedException(); }
public class DesastresClient(HttpClient http) : HttpPantallaClient(http), IDesastresClient
{
    SelectOption[] types=[];
    static readonly string[] Phases=["Detected","Registered","Assessment","Response","Recovery","Reconstruction","Closed"];
    public override async Task PrepararAsync(PantallaVm vm,IReadOnlyDictionary<string,string?> query,CancellationToken ct)
    {
        await base.PrepararAsync(vm,query,ct);
        types=Options(await Get("api/tipos-desastre",ct));
        vm.Screen=vm.Screen with { Api="Gestión de Desastres — servicio propietario",State='A' };
        Col[] history=[new("occurredAt","Fecha"),new("operation","Operación"),new("actor","Responsable"),new("version","Versión"),new("changes","Cambios registrados"),new("reason","Justificación"),new("result","Resultado")];
        if(vm.Screen.Id=="DIS-03")vm.Screen=vm.Screen with{Tables=[new("Historial",history,null)]};
        if(vm.Screen.Id=="DIS-08")vm.Screen=vm.Screen with{Cols=history,Tables=[]};
        if(vm.Screen.Id=="DIS-07")vm.Screen=vm.Screen with{Cols=[new("eventType","Evento durable"),new("occurredAt","Fecha"),new("version","Versión"),new("payload","Contenido")],Tables=[new("Historial",history,null)]};
        if(vm.Screen.Id=="DIS-05")vm.Screen=vm.Screen with{Cols=[new("id","Reporte"),new("sourceKind","Tipo de fuente"),new("sourceId","Fuente"),new("sourceReportId","Referencia de origen"),new("receivedAt","Recibido"),new("content","Contenido original")]};
        if(vm.Screen.Id=="DIS-06")vm.Screen=vm.Screen with{Cols=[new("organizationId","Organización (ID)"),new("responsibility","Responsabilidad")]};
        if(vm.Screen.Id=="DIS-04") {
            vm.Values.TryAdd("startedAt",DateTimeOffset.UtcNow.ToString("O"));
            Fields(F("nombre","Nombre",required:true),Select("disasterTypeId","Tipo",types,true),F("startedAt","Inicio",FT.DateTime,true),Select("attentionLevel","Atención",[new("1","Bajo"),new("2","Medio"),new("3","Alto"),new("4","Crítico")]),F("severidad","Severidad (1–5)",FT.Number,true),F("ubicacion","Municipio o ciudad",required:true),F("latitud","Latitud",FT.Number,true),F("longitud","Longitud",FT.Number,true),F("descripcion","Descripción",FT.TextArea,true));
        }
        if(vm.Screen.Id is "DIS-03" or "DIS-08") {
            var dialogs=new List<Dialog>{new("fase","Cambiar fase","Cambiar fase",[Select("phase","Fase",Phases.Select(x=>new SelectOption(x,x)).ToArray(),true),F("reason","Justificación",FT.TextArea,true),F("confirmNoCriticalCommitments","Confirmo que no hay compromisos críticos pendientes",FT.Bool)]),new("sev","Actualizar información","Actualizar información",[F("nombre","Nombre",required:true),F("ubicacion","Ubicación",required:true),F("descripcion","Descripción",FT.TextArea,true),F("severidad","Severidad",FT.Number,true),Select("disasterTypeId","Tipo",types),F("startedAt","Inicio",FT.DateTime),Select("attentionLevel","Atención",[new("1","Bajo"),new("2","Medio"),new("3","Alto"),new("4","Crítico")]),F("latitud","Latitud",FT.Number,true),F("longitud","Longitud",FT.Number,true),F("radio","Radio (m)",FT.Number)]),new("geo","Actualizar geografía","Actualizar geografía",[F("polygon","GeoJSON Polygon/MultiPolygon",FT.TextArea),F("municipalities","Municipios (uno por línea)",FT.TextArea),F("neighborhoods","Barrios (uno por línea)",FT.TextArea),F("riskZones","Zonas de riesgo (una por línea)",FT.TextArea),F("sourceId","Fuente")])};
            vm.Screen=vm.Screen with{Dialogs=dialogs.ToArray()};
        }
        if(vm.Screen.Id=="DIS-05")vm.Screen=vm.Screen with{Dialogs=[new("asociar","Consolidar reporte","Asociar reporte existente",[F("reportId","UUID del reporte",required:true)])]};
        if(vm.Screen.Id=="DIS-06")vm.Screen=vm.Screen with{Dialogs=[new("asociar","Asignar organismos","Asignar organismos",[F("registeringOrganizationId","Organismo registrador (ID)"),F("emergencyResponsibleId","Responsable (ID)"),F("organizations","Organismos: ID;responsabilidad (uno por línea)",FT.TextArea,true)])]};
        if(vm.Screen.Id=="DIS-07")vm.Screen=vm.Screen with{Dialogs=[]};
    }
    protected override async Task<JsonNode?> Read(Screen s,string? id,int sub,CancellationToken ct)
    {
        if(s.Id is "DIS-01" or "DIS-02") {
            var rows=await Get("api/desastres",ct);var list=Rows(rows);
            View.RealStats=true;View.Screen=View.Screen with{Stats=[new("Desastres",list.Length.ToString()),new("Activos",list.Count(x=>Text(x,"phase")!="Closed").ToString())]};
            Points(list);return rows;
        }
        var path="api/desastres/"+id;
        if(sub==-2 && s.Id is "DIS-05" or "DIS-06" or "DIS-07") {
            var current=await Get(path,ct);
            if(s.Id=="DIS-06")View.Values.TryAdd("organizations",string.Join('\n',Rows(current?["organizations"]).Select(x=>Text(x,"organizationId")+";"+Text(x,"responsibility"))));
            return current;
        }
        if(s.Id=="DIS-05")return await Get(path+"/reportes",ct);
        if(s.Id=="DIS-07"&&sub<0)return await Get(path+"/alertas",ct);
        if(sub>=0||s.Id is "DIS-07" or "DIS-08" && sub!=-2)return await Get(path+"/historial",ct);
        var detail=await Get(path,ct);Points(detail==null?[]:[detail]);
        if(s.Id=="DIS-06")return detail?["organizations"];
        if(detail?["geography"] is JsonObject geography) {
            View.Values.TryAdd("polygon",Text(geography,"polygon"));
            foreach(var name in new[]{"municipalities","neighborhoods","riskZones"})View.Values.TryAdd(name,string.Join('\n',Rows(geography[name]).Select(x=>x.ToString())));
        }
        if(detail is JsonObject obj){obj["tipoId"]=types.FirstOrDefault(x=>x.Value==Text(obj,"disasterTypeId"))?.Label;obj["inicio"]=obj["startedAt"]?.DeepClone();obj["actualizadoAt"]=obj["updatedAt"]?.DeepClone();obj["responsableId"]=obj["emergencyResponsibleId"]?.DeepClone();}
        return detail;
    }
    void Points(JsonNode[] list)=>View.PointsJson=System.Text.Json.JsonSerializer.Serialize(list.Select(x=>new{lat=x["latitud"],lng=x["longitud"],name=Text(x,"nombre"),sev=x["severidad"],radio=x["radio"],polygon=x["geography"]?["polygon"]}));
    protected override string Cell(JsonNode x,string key)=>key switch{"tipo"=>types.FirstOrDefault(t=>t.Value==Text(x,"disasterTypeId"))?.Label??"","inicio"=>Text(x,"startedAt"),"source"=>Text(x,"sourceId"),"reference"=>Text(x,"sourceReportId"),"description"=>Text(x,"content"),"name"=>Text(x,"organizationId"),"subject"=>Text(x,"eventType"),_=>base.Cell(x,key)};
    public Task<JsonNode?> SuggestionsAsync(int typeId,string location,DateTimeOffset startedAt,CancellationToken ct) =>
        Get("api/desastres/coincidencias?disasterTypeId="+typeId+"&ubicacion="+Uri.EscapeDataString(location)+"&startedAt="+Uri.EscapeDataString(startedAt.ToString("O")),ct);
    record DisasterInput(string Nombre,string Ubicacion,int Severidad,string Descripcion,double Latitud,double Longitud,double? Radio,int? DisasterTypeId,DateTimeOffset? StartedAt,int? AttentionLevel);
    record UpdateInput(string Nombre,string Ubicacion,int Severidad,string Descripcion,double Latitud,double Longitud,double? Radio,int? DisasterTypeId,DateTimeOffset? StartedAt,int? AttentionLevel,long ExpectedVersion);
    record PhaseInput(string Phase,long ExpectedVersion,string Reason,bool ConfirmNoCriticalCommitments);
    record Geography(JsonNode? Polygon,string[] Municipalities,string[] Neighborhoods,string[] RiskZones);
    record GeographyInput(long ExpectedVersion,Geography Geography,string? SourceId);
    record Participant(string OrganizationId,string Responsibility);
    record OrganizationsInput(long ExpectedVersion,string? RegisteringOrganizationId,string? EmergencyResponsibleId,Participant[] Organizations);
    static string[] Lines(string? x)=>(x??"").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
    public override async Task<string?> EnviarAsync(Screen s,string? dialog,Dictionary<string,string?> v,Guid key,string? id,CancellationToken ct)
    {
        var path="api/desastres/"+id;JsonNode? result;
        if(s.Id=="DIS-04") {
            var report=new DisasterInput(Required(v,"nombre"),Required(v,"ubicacion"),Integer(v,"severidad")??throw new ContractException("Falta severidad"),Required(v,"descripcion"),Number(v,"latitud")??throw new ContractException("Falta latitud"),Number(v,"longitud")??throw new ContractException("Falta longitud"),Number(v,"radio"),Integer(v,"disasterTypeId"),Date(v,"startedAt"),Integer(v,"attentionLevel"));
            if(Integer(v,"targetDisasterId") is int target) {
                if(target<=0 || !long.TryParse(v.GetValueOrDefault("targetVersion"),out var version) || version<1)throw new ContractException("Recarga las sugerencias y elige un desastre vigente.");
                result=await Send(HttpMethod.Post,$"api/desastres/{target}/reportes",new {report,expectedVersion=version},key,ct);
            }else result=await Send(HttpMethod.Post,"api/desastres",report,key,ct);
        }else if(dialog=="fase")result=await Send(HttpMethod.Put,path+"/fase",new PhaseInput(Required(v,"phase"),Version(v),Required(v,"reason"),v.GetValueOrDefault("confirmNoCriticalCommitments")=="true"),key,ct);
        else if(dialog=="sev")result=await Send(HttpMethod.Put,path,new UpdateInput(Required(v,"nombre"),Required(v,"ubicacion"),Integer(v,"severidad")??0,Required(v,"descripcion"),Number(v,"latitud")??0,Number(v,"longitud")??0,Number(v,"radio"),Integer(v,"disasterTypeId"),Date(v,"startedAt"),Integer(v,"attentionLevel"),Version(v)),key,ct);
        else if(dialog=="geo") {
            JsonNode? polygon=null;try{if(!string.IsNullOrWhiteSpace(v.GetValueOrDefault("polygon")))polygon=JsonNode.Parse(v["polygon"]!);}catch(System.Text.Json.JsonException){throw new ContractException("GeoJSON inválido");}
            result=await Send(HttpMethod.Put,path+"/geografia",new GeographyInput(Version(v),new(polygon,Lines(v.GetValueOrDefault("municipalities")),Lines(v.GetValueOrDefault("neighborhoods")),Lines(v.GetValueOrDefault("riskZones"))),v.GetValueOrDefault("sourceId")),key,ct);
        }else if(s.Id=="DIS-05")result=await Send(HttpMethod.Put,path+"/reportes/"+Identifier(v,"reportId"),new{expectedVersion=Version(v)},key,ct);
        else if(s.Id=="DIS-06"){
            var participants=Lines(v.GetValueOrDefault("organizations")).Select(x=>{var parts=x.Split(';',2);if(parts.Length!=2)throw new ContractException("Usa ID;responsabilidad");return new Participant(parts[0],parts[1]);}).ToArray();
            result=await Send(HttpMethod.Put,path+"/organismos",new OrganizationsInput(Version(v),v.GetValueOrDefault("registeringOrganizationId"),v.GetValueOrDefault("emergencyResponsibleId"),participants),key,ct);
        }else throw Unsupported();
        return Text(result,"id");
    }
}
