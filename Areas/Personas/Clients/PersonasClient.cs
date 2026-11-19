using System.Text.Json.Nodes;
using disaster_platform_web.Clients;
using disaster_platform_web.Screens;
namespace disaster_platform_web.Areas.Personas.Clients;
public interface IPersonasClient : IIntegrationClient { }
public class FakePersonasClient : FakePantallaClient, IPersonasClient { }
public class PersonasClient(HttpClient http) : HttpPantallaClient(http), IPersonasClient
{
    public override async Task PrepararAsync(PantallaVm vm,IReadOnlyDictionary<string,string?> query,CancellationToken ct)
    {
        await base.PrepararAsync(vm,query,ct);vm.Screen=vm.Screen with{Api="PR/DVR/CR mediante Integration Service",State='A'};
        if(string.IsNullOrEmpty(query.GetValueOrDefault("disasterId")))vm.Screen=vm.Screen with{Intro="Registros sin desastre asociado. Selecciona un desastre para consultar su ámbito autorizado."};
        if(vm.Screen.Id=="PEO-02")vm.Screen=vm.Screen with{Dialogs=[new("vincular","Vincular registro nativo","Vincular persona existente de Eden",[F("personId","ID nativo de persona",required:true),Select("disasterId","Desastre",vm.Disasters)])]};
        if(vm.Screen.Id=="PEO-03")Fields(InputFields());
        if(vm.Screen.Id=="PEO-04")vm.Screen=vm.Screen with{Tables=[new("Expedientes nativos",[new("caseId","ID del expediente")],null),new("Necesidades y atención",[new("subject","Asunto"),new("needDetails","Necesidad"),new("careDetails","Atención"),new("statusId","Estado nativo (ID)")],null)],Dialogs=[new("editar","Editar","Editar persona",InputFields())]};
        if(vm.Screen.Id is "PEO-05" or "PEO-06" or "PEO-07" or "PEO-08" or "PEO-09")Disable("Familias, expedientes, necesidades y alojamiento se gestionan mediante las interfaces nativas de Eden, con sus ACL.");
    }
    Field[] InputFields()=>[F("firstName","Nombres",required:true),F("lastName","Apellidos"),F("identificationRef","Identificación"),F("birthDate","Nacimiento",FT.Date),F("phone","Teléfono",FT.Phone),F("email","Correo",FT.Email),F("locationId","Ubicación Eden (ID)"),Select("disasterId","Desastre",View.Disasters)];
    protected override async Task<JsonNode?> Read(Screen s,string? id,int sub,CancellationToken ct)
    {
        if(View.Unsupported!=null)return new JsonArray();
        if(s.Id=="PEO-01") {
            var summary=await Get("api/personas-afectadas/resumen"+DisasterQuery(false),ct);
            View.RealStats=true;View.Screen=View.Screen with{Stats=[new("Personas",Text(summary,"peopleCount")),new("Familias",Text(summary,"familyCount")),new("Expedientes abiertos",Text(summary,"openCases")),new("Alojados",Text(summary,"shelteredCount"))]};return new JsonArray();
        }
        if(s.Id=="PEO-04") {var p=await Get("api/personas-afectadas/"+id,ct);return sub switch{0=>new JsonArray(Rows(p?["caseIds"]).Select(x=>(JsonNode)new JsonObject{["caseId"]=x.ToString()}).ToArray()),1=>p?["needs"],_=>p};}
        var query=DisasterQuery(false);query+=(query.Length==0?"?":"&")+"page="+Page().ToString()+"&size=25";
        if(Query.GetValueOrDefault("search") is {Length:>0} q)query+="&search="+Uri.EscapeDataString(q);
        var page=await Get("api/personas-afectadas"+query,ct);View.HasMore=page?["hasMore"]?.GetValue<bool>()==true;return page;
    }
    protected override string Cell(JsonNode x,string key)=>key=="nombre"?Text(x,"firstName")+" "+Text(x,"lastName"):base.Cell(x,key);
    record Binding(int? DisasterId);
    record Person(string FirstName,string? LastName,string? IdentificationRef,string? BirthDate,string? LocationId,string? Phone,string? Email,int? DisasterId);
    public override async Task<string?> EnviarAsync(Screen s,string? dialog,Dictionary<string,string?> v,Guid key,string? id,CancellationToken ct)
    {
        if(s.Id=="PEO-02" && dialog=="vincular") {
            var nativeId=Required(v,"personId");if(!long.TryParse(nativeId,out var person)||person<=0)throw new ContractException("Identificador nativo inválido.");
            var bound=await Send(HttpMethod.Post,$"api/personas-afectadas/{person}/vincular",new Binding(Integer(v,"disasterId")),key,ct);return Text(bound,"id");
        }
        if(s.Id is not "PEO-03" and not "PEO-04")throw Unsupported();
        var result=await Send(s.Id=="PEO-03"?HttpMethod.Post:HttpMethod.Put,"api/personas-afectadas"+(id==null?"":"/"+id),new Person(Required(v,"firstName"),Optional(v,"lastName"),Optional(v,"identificationRef"),Optional(v,"birthDate"),Optional(v,"locationId"),Optional(v,"phone"),Optional(v,"email"),Integer(v,"disasterId")),key,ct);
        return Text(result,"id");
    }
}
