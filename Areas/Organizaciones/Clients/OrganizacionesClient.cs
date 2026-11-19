using disaster_platform_web.Clients;
using disaster_platform_web.Screens;

namespace disaster_platform_web.Areas.Organizaciones.Clients;

public interface IOrganizacionesClient : IIntegrationClient { }

public class FakeOrganizacionesClient : FakePantallaClient, IOrganizacionesClient
{
    public override Task<IReadOnlyList<Row>?> FilasAsync(
        Screen s, Col[] cols, string? rowTo, string? id, int sub, CancellationToken ct)
    {
        if (s.Id == "ORG-01")
            return Task.FromResult<IReadOnlyList<Row>?>(OrganizacionesSeed.Organizations.Select(OrganizationRow).ToArray());

        if (s.Id == "ORG-04")
            return Task.FromResult<IReadOnlyList<Row>?>(OrganizacionesSeed.Tasks.Select(TaskRow).ToArray());

        if (s.Id == "ORG-02")
        {
            var organization = OrganizacionesSeed.FindOrganization(id);
            if (sub == 1 && organization is not null)
                return Task.FromResult<IReadOnlyList<Row>?>(OrganizacionesSeed.Tasks
                    .Where(task => task.OrganizationId.Equals(organization.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(TaskAssignmentRow).ToArray());
            if (sub == 2)
            {
                var integration = organization?.IntegrationDetails;
                IReadOnlyList<Row> rows = integration is null
                    ? []
                    : [new Row(null,
                    [
                        (integration.System, false), (integration.Direction, false), (integration.Mechanism, false),
                        (integration.Information, false), (integration.Modules, false)
                    ])];
                return Task.FromResult<IReadOnlyList<Row>?>(rows);
            }
        }

        return base.FilasAsync(s, cols, rowTo, id, sub, ct);
    }

    public override Task<IReadOnlyDictionary<string, string>?> DetalleAsync(Screen s, string? id, CancellationToken ct)
    {
        if (s.Id == "ORG-02" && OrganizacionesSeed.FindOrganization(id) is { } organization)
            return Task.FromResult<IReadOnlyDictionary<string, string>?>(new Dictionary<string, string>
            {
                ["name"] = organization.Name,
                ["acronym"] = organization.Acronym,
                ["category"] = organization.Category,
                ["scope"] = organization.Scope,
                ["country"] = organization.Country,
                ["location"] = organization.Location,
                ["phone"] = organization.Phone,
                ["email"] = organization.Email,
                ["description"] = organization.Description,
                ["actors"] = organization.Actors,
                ["channel"] = organization.Channel,
            });

        if (s.Id == "ORG-08" && OrganizacionesSeed.FindTask(id) is { } task)
            return Task.FromResult<IReadOnlyDictionary<string, string>?>(new Dictionary<string, string>
            {
                ["name"] = task.Name,
                ["description"] = task.Description,
                ["disaster"] = task.Disaster,
                ["organization"] = task.Organization,
                ["suggestedPriority"] = task.SuggestedPriority,
                ["confirmedPriority"] = task.ConfirmedPriority,
                ["status"] = task.Status,
                ["dueAt"] = task.DueAt,
                ["version"] = task.Version,
            });

        return base.DetalleAsync(s, id, ct);
    }

    static Row OrganizationRow(OrganizationSeed organization) =>
        new(Catalog.Url("ORG-02", organization.Id),
        [
            (organization.Name, false), (organization.Acronym, false), (organization.Category, false),
            (organization.Scope, false), (organization.Role, false), (organization.Integration, false),
            (organization.Status, true)
        ], organization.Id);

    static Row TaskRow(OrganizationTaskSeed task) =>
        new(Catalog.Url("ORG-08", task.Id),
        [
            (task.Name, false), (task.Disaster, false), (task.Organization, false),
            (task.SuggestedPriority, true), (task.ConfirmedPriority, true), (task.Status, true),
            (task.DueAt, false)
        ], task.Id);

    static Row TaskAssignmentRow(OrganizationTaskSeed task) =>
        new(Catalog.Url("ORG-08", task.Id),
        [
            (task.Name, false), (task.ConfirmedPriority, true), (task.Status, true), (task.DueAt, false)
        ], task.Id);
}

public class OrganizacionesClient(HttpClient http) : HttpPantallaClient(http), IOrganizacionesClient
{
    public override async Task PrepararAsync(PantallaVm vm,IReadOnlyDictionary<string,string?> query,CancellationToken ct)
    {
        await base.PrepararAsync(vm,query,ct);vm.Screen=vm.Screen with{Api="ORG/PROJECT/EVENT nativos mediante Integration Service",State='A'};
        if(vm.Screen.Id is "ORG-02" or "ORG-03") {
            var types=Options(await Get("api/organizaciones/tipos?tamanio=100",ct));
            var fields=new[]{F("name","Nombre",required:true),F("acronym","Sigla"),F("country","País ISO (CO)"),Select("typeId","Tipo nativo",types),F("locationId","Ubicación Eden (ID)"),F("phone","Teléfono",FT.Phone),F("email","Correo",FT.Email),F("description","Descripción",FT.TextArea)};
            if(vm.Screen.Id=="ORG-03")Fields(fields);
            else vm.Screen=vm.Screen with{Cols=[new("name","Nombre"),new("acronym","Sigla"),new("typeIds","Tipos nativos"),new("country","País"),new("locationId","Ubicación (ID)"),new("phone","Teléfono"),new("email","Correo"),new("description","Descripción")],Dialogs=[new("editar","Editar","Editar organización",fields)],Tables=[new("Personal nativo",[new("id","ID")],null),new("Voluntarios nativos",[new("id","ID")],null),new("Instalaciones nativas",[new("id","ID")],null)]};
        }
        if(vm.Screen.Id=="ORG-01")vm.Screen=vm.Screen with{Cols=[new("name","Organización"),new("acronym","Sigla"),new("typeIds","Tipos nativos"),new("country","País"),new("locationId","Ubicación (ID)")]};
        if(vm.Screen.Id is "ORG-04" or "ORG-05" or "ORG-08") {
            bool task=vm.Screen.Id!="ORG-05";
            vm.Screen=vm.Screen with{Cols=task?[new("id","ID"),new("name","Tarea"),new("projectId","Proyecto Eden"),new("disasterId","Desastre"),new("organizationId","Organización"),new("priorityId","Prioridad (ID)"),new("statusId","Estado (ID)"),new("assignee","Responsable"),new("dueAt","Vencimiento")]:[new("id","ID"),new("name","Actividad"),new("projectId","Proyecto Eden"),new("disasterId","Desastre"),new("statusId","Estado (ID)"),new("contactPersonId","Contacto"),new("startsOn","Inicio"),new("endsOn","Fin"),new("comments","Observaciones")]};
            var fields=new List<Field>{Select("disasterId","Desastre",vm.Disasters,true),F("projectId","Proyecto Eden (ID)",required:true),F("name","Nombre",required:true),F("statusId","Estado nativo (ID)",required:true)};
            if(task)fields.AddRange([F("description","Descripción",FT.TextArea),F("priorityId","Prioridad nativa (ID)",required:true),F("assigneeId","Responsable Eden (ID)"),Select("assigneeKind","Tipo de responsable",[new("person","Persona"),new("organization","Organización"),new("team","Equipo")]),F("dueAt","Vencimiento",FT.DateTime),F("timeEstimated","Horas estimadas",FT.Number),F("activityId","Actividad Eden relacionada (ID)")]);
            else fields.AddRange([F("contactPersonId","Contacto Eden (ID)"),F("locationId","Ubicación Eden (ID)"),F("startsOn","Inicio",FT.Date),F("endsOn","Fin",FT.Date),F("comments","Observaciones",FT.TextArea)]);
            var dialogs=new List<Dialog>{new(vm.Screen.Id=="ORG-08"?"editar":"crear",vm.Screen.Id=="ORG-08"?"Editar":"Crear",task?"Tarea nativa Eden":"Actividad nativa Eden",fields.ToArray())};
            if(!task)dialogs.Add(new("editar","Editar actividad","Actualizar actividad Eden",new[]{F("id","ID de la actividad",required:true)}.Concat(fields).ToArray()));
            vm.Screen=vm.Screen with{Dialogs=dialogs.ToArray()};
        }
        if(vm.Screen.Id is "ORG-06" or "ORG-07")Disable("No existe un contrato de mensajería o bitácora global para esta pantalla.");
    }
    protected override async Task<System.Text.Json.Nodes.JsonNode?> Read(Screen s,string? id,int sub,CancellationToken ct)
    {
        if(View.Unsupported!=null)return new System.Text.Json.Nodes.JsonArray();
        if(s.Id=="ORG-02") {
            var org=await Get("api/organizaciones/"+id,ct);
            if(sub>=0){var field=sub switch{0=>"personnelIds",1=>"volunteerIds",_=>"facilityIds"};return new System.Text.Json.Nodes.JsonArray(Rows(org?[field]).Select(x=>(System.Text.Json.Nodes.JsonNode)new System.Text.Json.Nodes.JsonObject{["id"]=x.ToString()}).ToArray());}return org;
        }
        if(s.Id is "ORG-04" or "ORG-08" or "ORG-05") {
            var result=await Get("api/coordinacion/"+(s.Id=="ORG-05"?"actividades":"tareas")+DisasterQuery()+"&page="+Page().ToString()+"&size=25",ct);
            View.HasMore=result?["hasMore"]?.GetValue<bool>()==true;
            if(s.Id=="ORG-08") {var found=Rows(result).FirstOrDefault(x=>Text(x,"id")==id)??throw new HttpRequestException("Tarea fuera de la página seleccionada",null,System.Net.HttpStatusCode.NotFound);found["assigneeId"]=found["assignee"]?["id"]?.DeepClone();found["assigneeKind"]=found["assignee"]?["kind"]?.DeepClone();return found;}return result;
        }
        var route="api/organizaciones?pagina="+Page().ToString()+"&tamanio=25&q="+Uri.EscapeDataString(Query.GetValueOrDefault("search")??"");
        var page=await Get(route,ct);View.HasMore=page?["hasMore"]?.GetValue<bool>()==true;return page;
    }
    protected override string Cell(System.Text.Json.Nodes.JsonNode x,string key)=>key switch{"organization"=>Text(x,"organizationId"),"disaster"=>Text(x,"disasterId"),"status"=>Text(x,"statusId"),"confirmedPriority"=>Text(x,"priorityId"),"startsAt"=>Text(x,"startsOn"),"endsAt"=>Text(x,"endsOn"),"contact"=>Text(x,"contactPersonId"),_=>base.Cell(x,key)};
    record Organization(string Name,string? Acronym,string? Country,string? TypeId,string? LocationId,string? Phone,string? Email,string? Description);
    record Assignee(string Kind,string Id);
    record TaskInput(int DisasterId,string ProjectId,string Name,string StatusId,string PriorityId,string? Description,Assignee? Assignee,DateTimeOffset? DueAt,decimal? TimeEstimated,string? ActivityId);
    record ActivityInput(int DisasterId,string ProjectId,string Name,string StatusId,string? ContactPersonId,string? LocationId,string? StartsOn,string? EndsOn,string? Comments);
    public override async Task<string?> EnviarAsync(Screen s,string? dialog,Dictionary<string,string?> v,Guid key,string? id,CancellationToken ct)
    {
        System.Text.Json.Nodes.JsonNode? result;
        if(s.Id is "ORG-03" or "ORG-02")result=await Send(id==null?HttpMethod.Post:HttpMethod.Put,"api/organizaciones"+(id==null?"":"/"+id),new Organization(Required(v,"name"),Optional(v,"acronym"),Optional(v,"country"),Optional(v,"typeId"),Optional(v,"locationId"),Optional(v,"phone"),Optional(v,"email"),Optional(v,"description")),key,ct);
        else if(s.Id is "ORG-04" or "ORG-08")result=await Send(id==null?HttpMethod.Post:HttpMethod.Put,"api/coordinacion/tareas"+(id==null?"":"/"+id),new TaskInput(Integer(v,"disasterId")??throw new ContractException("Falta desastre"),Required(v,"projectId"),Required(v,"name"),Required(v,"statusId"),Required(v,"priorityId"),Optional(v,"description"),string.IsNullOrWhiteSpace(Optional(v,"assigneeId"))?null:new Assignee(Required(v,"assigneeKind"),Required(v,"assigneeId")),Date(v,"dueAt"),string.IsNullOrWhiteSpace(Optional(v,"timeEstimated"))?null:Money(v,"timeEstimated"),Optional(v,"activityId")),key,ct);
        else if(s.Id=="ORG-05")result=await Send(dialog=="editar"?HttpMethod.Put:HttpMethod.Post,"api/coordinacion/actividades"+(dialog=="editar"?"/"+Required(v,"id"):""),new ActivityInput(Integer(v,"disasterId")??throw new ContractException("Falta desastre"),Required(v,"projectId"),Required(v,"name"),Required(v,"statusId"),Optional(v,"contactPersonId"),Optional(v,"locationId"),Optional(v,"startsOn"),Optional(v,"endsOn"),Optional(v,"comments")),key,ct);
        else throw Unsupported();return Text(result,"id");
    }
}
