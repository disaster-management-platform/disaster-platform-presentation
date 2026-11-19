using System.Text.Json;
using System.Text.Json.Nodes;
using disaster_platform_web.Clients;
using disaster_platform_web.Screens;
namespace disaster_platform_web.Areas.Reconstruccion.Clients;
public interface IReconstruccionClient : IPantallaClient { Task<HttpResponseMessage> DownloadAsync(string id,string attachmentId,CancellationToken ct); }
public class FakeReconstruccionClient : FakePantallaClient, IReconstruccionClient { public Task<HttpResponseMessage> DownloadAsync(string id,string attachmentId,CancellationToken ct)=>throw new NotSupportedException(); }
public class ReconstruccionClient(HttpClient http) : HttpPantallaClient(http), IReconstruccionClient
{
    const string Root="api/reconstruccion/";
    JsonNode? project;
    public override async Task PrepararAsync(PantallaVm vm,IReadOnlyDictionary<string,string?> query,CancellationToken ct)
    {
        await base.PrepararAsync(vm,query,ct);vm.Screen=vm.Screen with{Api="Reconstrucción — contratos publicados",State='A'};
        if (string.IsNullOrWhiteSpace(query.GetValueOrDefault("disasterId")) && vm.Disasters.OrderBy(disaster => int.Parse(disaster.Value)).FirstOrDefault() is { } selected)
        {
            Query = new Dictionary<string,string?>(query) { ["disasterId"] = selected.Value };
            vm.Values["disasterId"] = selected.Value;
        }
        if(vm.RouteId!=null&&vm.Screen.Id is not "REC-03") {
            project=await Get(Root+"proyectos/"+vm.RouteId,ct);vm.ProjectStatus=Text(project,"status");
            foreach(var entry in project!.AsObject())
                if (entry.Key != "activities" || vm.Screen.Id != "REC-06") vm.Values.TryAdd(entry.Key,entry.Value?.ToString());
            vm.Values.TryAdd("expectedVersion",Text(project,"version"));
        }
        if(vm.Screen.Id=="REC-13")vm.Screen=vm.Screen with{Cols=new[]{new Col("status","Estado"),new Col("closedAt","Cerrado")}.Concat(vm.Screen.Cols.Where(c=>c.Key!="releasedBudget")).ToArray()};
        if(vm.Screen.Id=="REC-03")vm.Screen=vm.Screen with{Links=vm.Screen.Links.Where(l=>l.To!="REC-05").ToArray()};
        if(vm.Screen.Id=="REC-02")vm.Screen=vm.Screen with{Dialogs=vm.Screen.Dialogs.Select(x=>x.Id=="generar"?x with{Fields=[Select("disasterId","Desastre",vm.Disasters,true)]}:x).ToArray()};
        if(vm.Screen.Id=="REC-06") {
            var funding=await Get(Root+"financiacion?disasterId="+Text(project,"disasterId"),ct);
            Fields(F("finalBudget","Presupuesto final (COP)",FT.Number,true),F("fundingSource","Fuente de financiación",required:true),F("fundingSourceId","Identificador de la fuente",required:true),F("responsible","Responsable",required:true),F("contractor","Contratista",required:true),F("activities","Cronograma",FT.TextArea,true));
            if(Text(project,"fundingSource").Length==0)vm.Values["fundingSource"]=Text(funding,"source");
            if(!vm.Values.ContainsKey("fundingSourceId") || string.IsNullOrEmpty(vm.Values["fundingSourceId"]))vm.Values["fundingSourceId"]=Text(funding,"sourceId");
            vm.Values.TryAdd("activities",string.Join('\n',Rows(project?["activities"]).Select(x=>$"{Text(x,"name")};{Text(x,"responsible")};{Text(x,"startsAt")[..10]};{Text(x,"dueAt")[..10]};{Text(x,"budget")}")));
            vm.Screen=vm.Screen with{Intro="Financiación: "+Text(funding,"source")+". Disponible: "+Text(funding,"availableAmount")+" COP. La financiación DEMO es simulada."};
        }
        if(vm.Screen.Id=="REC-07")Fields(F("contractReference","Referencia del contrato (o adjunta el PDF)"),F("contract","Contrato PDF firmado (sintético para demo)",FT.File),F("signedAt","Fecha de firma",FT.DateTime,true));
        if(vm.Screen.Id=="REC-12")vm.Screen=vm.Screen with{Fields=vm.Screen.Fields.Select(f=>f.Name=="activityId"?Select("activityId","Actividad",Options(project?["activities"]),true):f).ToArray()};
        if(vm.Screen.Id is "REC-08" or "REC-09" or "REC-10" or "REC-14" or "REC-15") {
            vm.Screen=vm.Screen with{Dialogs=[]};
            if(vm.Screen.Id!="REC-08")Disable("Este proyecto no dispone de un contrato para esta operación. La coordinación operativa se realiza en Eden / Coordinación de organismos.");
        }
    }
    async Task<JsonNode?> Proposal(string? id,CancellationToken ct)=>Rows(await Get(Root+"propuestas"+DisasterQuery(),ct)).FirstOrDefault(x=>Text(x,"id")==id)??throw new HttpRequestException("Propuesta no encontrada",null,System.Net.HttpStatusCode.NotFound);
    protected override async Task<JsonNode?> Read(Screen s,string? id,int sub,CancellationToken ct)
    {
        if(View.Unsupported!=null)return new JsonArray();
        if(s.Id is "REC-01" or "REC-02") {
            var proposals=await Get(Root+"propuestas"+DisasterQuery(),ct);
            if(s.Id=="REC-01") {
                var projects=Rows(await Get(Root+"proyectos"+DisasterQuery(),ct));
                var funding=await Get(Root+"financiacion"+DisasterQuery(),ct);
                View.RealStats=true;View.Screen=View.Screen with{Stats=[new("Propuestas",Rows(proposals).Length.ToString()),new("Proyectos",projects.Length.ToString()),new("Disponibilidad (COP)",Text(funding,"availableAmount"))],Intro=Text(funding,"source")};
            }
            return proposals;
        }
        if(s.Id=="REC-03") {
            var proposal=await Proposal(id,ct);if(sub>=0)return proposal?["evaluations"];
            var funding=await Get(Root+"financiacion?disasterId="+Text(proposal,"disasterId"),ct);
            proposal!["availableBudget"]=funding?["availableAmount"]?.DeepClone();
            proposal["coversBudget"]=proposal["proposedBudget"]!.GetValue<decimal>()<=funding!["availableAmount"]!.GetValue<decimal>();
            return proposal;
        }
        if(s.Id=="REC-04")return await Get(Root+"proyectos"+DisasterQuery(false),ct);
        var path=Root+"proyectos/"+id;
        if(s.Id=="REC-05"&&sub==2)return await Get(path+"/auditoria",ct);
        if(s.Id=="REC-05"&&sub==3)return await Get(path+"/linea-tiempo",ct);
        var monitor=await Get(path+"/monitoreo",ct);var pjt=monitor?["project"];var summary=monitor?["summary"];var activities=Rows(pjt?["activities"]);
        View.RealStats=true;View.Screen=View.Screen with{Stats=[new("Presupuesto",Text(pjt,"finalBudget")),new("Ejecutado",Text(summary,"totalCost")),new("Saldo",Text(summary,"remainingBudget"))]};
        if(s.Id=="REC-05"&&sub==1)return new JsonArray(activities.Select(a=>new JsonObject{["activity"]=Text(a,"name"),["status"]=a["status"]?.DeepClone(),["actualCost"]=a["actualCost"]?.DeepClone(),["reportCount"]=Rows(a["reports"]).Length,["evidenceCount"]=Rows(a["reports"]).Sum(r=>Rows(r["evidence"]).Length),["complete"]=a["isResolved"]?.DeepClone()}).ToArray());
        if(s.Id=="REC-12")return new JsonArray(activities.SelectMany(a=>Rows(a["reports"]).Select(r=>{var copy=r.DeepClone();copy["activity"]=Text(a,"name");copy["adjuntos"]=string.Join(", ",Rows(r["evidence"]).Concat(Rows(r["documents"])).Select(x=>x.ToString()));return copy;})).ToArray());
        if(sub>=0||s.Id is "REC-08" or "REC-11")return pjt?["activities"];
        var merged=pjt!.DeepClone().AsObject();foreach(var entry in summary!.AsObject())merged[entry.Key]=entry.Value?.DeepClone();
        merged["totalExecuted"]=summary["totalCost"]?.DeepClone();merged["totalReports"]=summary["reportsCount"]?.DeepClone();merged["activitiesWithoutReports"]=activities.Count(a=>Rows(a["reports"]).Length==0);
        return merged;
    }
    public async Task<HttpResponseMessage> DownloadAsync(string id,string attachmentId,CancellationToken ct)
    {
        var response=await Http.GetAsync($"{Root}proyectos/{id}/adjuntos/{attachmentId}",HttpCompletionOption.ResponseHeadersRead,ct);
        if(!response.IsSuccessStatusCode){response.Dispose();throw new HttpRequestException("No se pudo descargar el adjunto",null,response.StatusCode);}
        return response;
    }
    protected override IReadOnlyList<AttachmentLink> DownloadLinks(Screen s,JsonNode item,string? parent,int sub) => s.Id=="REC-12" ? Rows(item["evidence"]).Concat(Rows(item["documents"])).Select(x=>x.ToString()).Where(x=>x.StartsWith("attachment:")&&Guid.TryParse(x[11..],out _)).Select(x=>new AttachmentLink($"/Reconstruccion/Proyectos/{parent}/Adjuntos/{x[11..]}",x)).ToArray() : [];
    protected override string Cell(JsonNode x,string key)=>key switch{"name"=>Text(x,"name","id"),"resultado"=>Text(x,"priority") switch{"High"=>"Priorizado","Low"=>"Prioridad baja",_=>"Sin asignación"},"reportes"=>Rows(x["reports"]).Length.ToString(),"evidencias"=>Rows(x["reports"]).Sum(r=>Rows(r["evidence"]).Length).ToString(),"accion"=>Text(x,"action"),_=>base.Cell(x,key)};
    record SelectProposal(Guid ProposalId,long ExpectedVersion);
    record Activity(string Name,string Responsible,DateTimeOffset StartsAt,DateTimeOffset DueAt,decimal Budget);
    record Formalization(decimal FinalBudget,string FundingSource,string FundingSourceId,string Responsible,string Contractor,Activity[] Activities,long ExpectedVersion);
    record SignedContract(string ContractReference,DateTimeOffset SignedAt,long ExpectedVersion);
    record Progress(string Status,decimal ActualCost,string[] Evidence,string[] Documents,long ExpectedVersion,Guid RequestId);
    public override async Task<string?> EnviarAsync(Screen s,string? dialog,Dictionary<string,string?> v,Guid key,string? id,CancellationToken ct)
    {
        JsonNode? result;var path=Root+"proyectos/"+id;
        if(s.Id=="REC-02"&&dialog=="generar")result=await Send(HttpMethod.Post,Root+"propuestas/generar",new{disasterId=Integer(v,"disasterId")??throw new ContractException("Selecciona un desastre")},key,ct);
        else if(s.Id is "REC-02" or "REC-03"){
            var proposal=await Proposal(id,ct);
            result=await Send(HttpMethod.Post,Root+"proyectos",new SelectProposal(Guid.Parse(id!),proposal!["version"]!.GetValue<long>()),key,ct);
            return Text(result,"id");
        }else if(s.Id=="REC-06"&&dialog=="confirmar")result=await Send(HttpMethod.Post,path+"/confirmar-formalizacion",new{expectedVersion=Version(v)},key,ct);
        else if(s.Id=="REC-06") {
            var activities=Required(v,"activities").Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Select(line=>{var p=line.Split(';');if(p.Length!=5)throw new ContractException("Cronograma inválido");return new Activity(p[0],p[1],DateTimeOffset.Parse(p[2]+"T00:00:00Z"),DateTimeOffset.Parse(p[3]+"T00:00:00Z"),decimal.Parse(p[4],System.Globalization.CultureInfo.InvariantCulture));}).ToArray();
            result=await Send(HttpMethod.Put,path+"/formalizacion",new Formalization(Money(v,"finalBudget"),Required(v,"fundingSource"),Required(v,"fundingSourceId"),Required(v,"responsible"),Required(v,"contractor"),activities,Version(v)),key,ct);
        }else if(s.Id=="REC-07") {
            var reference=v.GetValueOrDefault("contractReference");
            if(View.Files.Count>1)throw new ContractException("Adjunta un solo contrato.");
            if(View.Files.Count==1) {var uploaded=await Upload(path+"/adjuntos",View.Files[0],"document",key,false,ct);var attachment=Text(uploaded,"id");if(!Guid.TryParse(attachment,out _))throw new ContractException("El contrato no confirmó su adjunto.");reference="attachment:"+attachment;}
            if(string.IsNullOrWhiteSpace(reference))throw new ContractException("Indica la referencia o adjunta el contrato firmado.");
            result=await Send(HttpMethod.Post,path+"/contrato-firmado",new SignedContract(reference,Date(v,"signedAt")??throw new ContractException("Falta fecha"),Version(v)),key,ct);
        }
        else if(s.Id=="REC-11")result=await Send(HttpMethod.Put,path+"/presupuesto",new{budget=Money(v,"budget"),reason=Required(v,"reason"),expectedVersion=Version(v)},key,ct);
        else if(s.Id=="REC-13")result=await Send(HttpMethod.Post,path+"/cerrar",new{expectedVersion=Version(v)},key,ct);
        else if(s.Id=="REC-12") {
            var current=await Get(path,ct);
            var activity=Rows(current?["activities"]).FirstOrDefault(a=>Text(a,"id")==Required(v,"activityId"))??throw new ContractException("Actividad no disponible.");
            var previous=Rows(activity["reports"]).FirstOrDefault(r=>Text(r,"requestId")==key.ToString());
            if(previous!=null) {
                if(Text(previous,"status")!=Required(v,"status") || previous["actualCost"]!.GetValue<decimal>()!=Money(v,"actualCost"))throw new HttpRequestException("La operación ya existe con otro contenido",null,System.Net.HttpStatusCode.Conflict);
                foreach(var category in new[]{"evidence","documents"}) {
                    var files=View.Files.Where(f=>f.Name=="Values["+category+"]").ToArray();var refs=Rows(previous[category]);
                    if(files.Length!=refs.Length)throw new HttpRequestException("Adjuntos distintos para la misma operación",null,System.Net.HttpStatusCode.Conflict);
                    for(var i=0;i<files.Length;i++) {
                        var reference=refs[i].ToString();if(!reference.StartsWith("attachment:")||!Guid.TryParse(reference[11..],out _))throw new ContractException("Adjunto histórico sin descarga disponible.");
                        using var remote=await DownloadAsync(id!,reference[11..],ct);using var remoteStream=await remote.Content.ReadAsStreamAsync(ct);using var local=files[i].OpenReadStream();
                        var remoteHash=await System.Security.Cryptography.SHA256.HashDataAsync(remoteStream,ct);var localHash=await System.Security.Cryptography.SHA256.HashDataAsync(local,ct);
                        if(!remoteHash.SequenceEqual(localHash))throw new HttpRequestException("Adjunto incompatible",null,System.Net.HttpStatusCode.Conflict);
                    }
                }
                return id;
            }
            var evidence=new List<string>();var documents=new List<string>();
            foreach(var file in View.Files){var category=file.Name=="Values[evidence]"?"evidence":"document";var uploaded=await Upload(path+"/adjuntos",file,category,key,false,ct);var reference="attachment:"+Text(uploaded,"id");if(!reference.StartsWith("attachment:"))throw new ContractException("Adjunto sin referencia confirmada");(category=="evidence"?evidence:documents).Add(reference);}
            result=await Send(HttpMethod.Post,path+"/actividades/"+Guid.Parse(Required(v,"activityId"))+"/reportes",new Progress(Required(v,"status"),Money(v,"actualCost"),evidence.ToArray(),documents.ToArray(),Version(v),key),key,ct);
        }else throw Unsupported();
        return id;
    }
}
