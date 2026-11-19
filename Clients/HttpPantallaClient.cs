using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using disaster_platform_web.Screens;

namespace disaster_platform_web.Clients;

public abstract class HttpPantallaClient(HttpClient http) : IPantallaClient
{
    protected HttpClient Http => http;
    protected IReadOnlyDictionary<string, string?> Query { get; set; } = new Dictionary<string, string?>();
    protected PantallaVm View { get; private set; } = default!;
    public bool Simulado => false;
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public virtual async Task PrepararAsync(PantallaVm vm, IReadOnlyDictionary<string, string?> query, CancellationToken ct)
    {
        View = vm; Query = query;
        foreach (var (key, value) in query) vm.Values.TryAdd(key, value);
        if(vm.Screen.Id is not "ORG-01" and not "ORG-02" and not "ORG-03") {
            try { vm.Disasters=Rows(await Get("api/desastres",ct)).Select(x=>new SelectOption(Text(x,"id"),Text(x,"nombre"))).ToArray(); }
            catch(HttpRequestException) { vm.FlashError??="El selector de desastres no está disponible."; }
            catch(TaskCanceledException) when(!ct.IsCancellationRequested) { vm.FlashError??="El selector de desastres agotó su tiempo de espera."; }
            if(vm.Disasters.Length==0 && int.TryParse(vm.Values.GetValueOrDefault("disasterId"),out var known) && known>0)vm.Disasters=[new(known.ToString(),"Desastre ID "+known+" — nombre no disponible")];
        }
        vm.Screen = vm.Screen with { Filters = vm.Screen.Filters.Where(f => f.Name is not "disaster" and not "disasterId").ToArray() };
    }
    protected abstract Task<JsonNode?> Read(Screen screen, string? id, int sub, CancellationToken ct);
    public abstract Task<string?> EnviarAsync(Screen s, string? dialog, Dictionary<string, string?> values, Guid key, string? id, CancellationToken ct);
    public async Task<IReadOnlyList<Row>?> FilasAsync(Screen s, Col[] cols, string? rowTo, string? id, int sub, CancellationToken ct)
    {
        var items = Rows(await Read(s,id,sub,ct));
        View.Screen=View.Screen with { Filters=View.Screen.Filters.Select(f => f with { Options=items.Select(x=>Cell(x,f.Name)).Where(x=>x.Length>0).Distinct().Order().ToArray() }).ToArray() };
        return items.Select(item => {
            var recordId = Text(item, "evaluationId", "id", "projectId");
            var href = rowTo == null || recordId.Length == 0 ? null : Catalog.Url(rowTo, recordId);
            if (href != null && Query.GetValueOrDefault("disasterId") is { } disaster) href += "?disasterId=" + Uri.EscapeDataString(disaster);
            if(href!=null && Query.ContainsKey("page"))href+=(href.Contains('?')?"&":"?")+"page="+Page();
            return new Row(href, cols.Select(c => (Cell(item,c.Key),c.Type == CT.Badge)).ToArray(),recordId, DownloadLinks(s,item,id,sub));
        }).ToArray();
    }
    public virtual async Task<IReadOnlyDictionary<string,string>?> DetalleAsync(Screen s,string? id,CancellationToken ct)
    {
        var value = await Read(s,id,-2,ct);
        return value is JsonObject obj ? obj.ToDictionary(x=>x.Key,x=>x.Value?.ToString() ?? "",StringComparer.OrdinalIgnoreCase) : null;
    }
    protected virtual IReadOnlyList<AttachmentLink> DownloadLinks(Screen s,JsonNode item,string? parent,int sub) => [];
    protected virtual string Cell(JsonNode item,string key) => Text(item,key);
    protected static string? Optional(Dictionary<string,string?> values,string name) => string.IsNullOrWhiteSpace(values.GetValueOrDefault(name))?null:values[name];
    protected async Task<JsonNode?> Get(string route,CancellationToken ct)
    {
        using var response = await http.GetAsync(route,ct); response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonNode>(Json,ct);
    }
    protected async Task<JsonNode?> Send<T>(HttpMethod method,string route,T body,Guid key,CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method,route) { Content = JsonContent.Create(body,options:Json) };
        request.Headers.Add("Idempotency-Key",key.ToString());
        using var response=await http.SendAsync(request,ct); response.EnsureSuccessStatusCode();
        return response.StatusCode == HttpStatusCode.NoContent ? null : await response.Content.ReadFromJsonAsync<JsonNode>(Json,ct);
    }
    protected async Task<JsonNode?> Upload(string route,IFormFile file,string category,Guid key,bool damage,CancellationToken ct)
    {
        using var form=new MultipartFormDataContent();
        if(damage) {
            using var hashStream=file.OpenReadStream();
            form.Add(new StringContent(Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(hashStream,ct)).ToLowerInvariant()),"sha256");
            form.Add(new StringContent(file.Length.ToString(CultureInfo.InvariantCulture)),"size");
        } else form.Add(new StringContent(category),"category");
        var content=new StreamContent(file.OpenReadStream()); content.Headers.ContentType=new(file.ContentType);
        form.Add(content,"file",Path.GetFileName(file.FileName));
        using var request=new HttpRequestMessage(HttpMethod.Post,route){Content=form};request.Headers.Add("Idempotency-Key",key.ToString());
        using var response=await http.SendAsync(request,ct);response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonNode>(Json,ct);
    }
    protected static JsonNode[] Rows(JsonNode? node) => node switch {
        JsonArray array => array.Where(x=>x!=null).Select(x=>x!).ToArray(),
        JsonObject obj when obj["items"] is JsonArray items => Rows(items),
        JsonObject obj when obj["evaluations"] is JsonArray items => Rows(items),
        _ => []
    };
    protected static string Text(JsonNode? node,params string[] keys) { foreach(var key in keys) if(node is JsonObject obj && obj[key] is { } value)return value.ToString(); return ""; }
    protected static string Required(Dictionary<string,string?> v,string name) => string.IsNullOrWhiteSpace(v.GetValueOrDefault(name)) ? throw new ContractException($"Falta {name}.") : v[name]!;
    protected static Guid Identifier(Dictionary<string,string?> values,string name) => Guid.TryParse(Required(values,name),out var id)&&id!=Guid.Empty?id:throw new ContractException($"{name} debe ser UUID.");
    protected static int? Integer(Dictionary<string,string?> v,string name) => string.IsNullOrWhiteSpace(v.GetValueOrDefault(name)) ? null : int.TryParse(v[name],NumberStyles.Integer,CultureInfo.InvariantCulture,out var x)?x:throw new ContractException($"{name} inválido.");
    protected static decimal Money(Dictionary<string,string?> v,string name) => decimal.TryParse(Required(v,name),NumberStyles.Number,CultureInfo.InvariantCulture,out var x)?x:throw new ContractException($"{name} inválido.");
    protected static double? Number(Dictionary<string,string?> v,string name) => string.IsNullOrWhiteSpace(v.GetValueOrDefault(name))?null:double.TryParse(v[name],NumberStyles.Float,CultureInfo.InvariantCulture,out var x)&&double.IsFinite(x)?x:throw new ContractException($"{name} inválido.");
    protected static DateTimeOffset? Date(Dictionary<string,string?> v,string name)
    {
        if(string.IsNullOrWhiteSpace(v.GetValueOrDefault(name)))return null;
        if(!DateTimeOffset.TryParse(v[name],CultureInfo.InvariantCulture,DateTimeStyles.AssumeLocal,out var value))throw new ContractException($"{name} inválido.");
        if(DateTimeOffset.TryParse(v.GetValueOrDefault("display_"+name),CultureInfo.InvariantCulture,DateTimeStyles.AssumeLocal,out var displayed) && displayed==value && DateTimeOffset.TryParse(v.GetValueOrDefault("original_"+name),CultureInfo.InvariantCulture,DateTimeStyles.AssumeLocal,out var original))return original;
        return value;
    }
    protected static long Version(Dictionary<string,string?> v) => long.TryParse(v.GetValueOrDefault("expectedVersion"),out var x)&&x>=0?x:throw new ContractException("Recarga la versión antes de guardar.");
    protected int Page() => !Query.TryGetValue("page",out var raw) || string.IsNullOrEmpty(raw) ? 0 : int.TryParse(raw,out var page) && page>=0 ? page : throw new ContractException("Página inválida.");
    protected string DisasterQuery(bool required=true)
    {
        var value=Query.GetValueOrDefault("disasterId");
        if(string.IsNullOrWhiteSpace(value))return required?throw new ContractException("Selecciona un desastre."):"";
        return int.TryParse(value,out var id)&&id>0?"?disasterId="+id:throw new ContractException("El identificador del desastre es inválido.");
    }
    protected static SelectOption[] Options(JsonNode? node,string label="name") => Rows(node).Select(x=>new SelectOption(Text(x,"id"),Text(x,label))).ToArray();
    protected void Fields(params Field[] fields) => View.Screen=View.Screen with { Fields=fields };
    protected static Field F(string name,string label,FT type=FT.Text,bool required=false) => new(){Name=name,Label=label,Type=type,Required=required};
    protected static Field Select(string name,string label,SelectOption[] choices,bool required=false) => F(name,label,FT.Select,required) with {Choices=choices};
    protected static HttpRequestException Unsupported() => new("Esta operación utiliza el flujo nativo o no tiene contrato disponible.",null,HttpStatusCode.NotImplemented);
    protected void Disable(string reason) { View.Unsupported=reason;View.Screen=View.Screen with{Dialogs=[],Fields=[]}; }
}
public sealed class ContractException(string message) : HttpRequestException(message,null,HttpStatusCode.UnprocessableEntity);
