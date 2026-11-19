#:project ../disaster-platform-web.csproj
#:property JsonSerializerIsReflectionEnabledByDefault=true
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using disaster_platform_web.Areas.Desastres.Clients;
using disaster_platform_web.Areas.Desastres.Controllers;
using disaster_platform_web.Clients;
using disaster_platform_web.Screens;

var handler = new Responses();
using var http = new HttpClient(handler) { BaseAddress=new Uri("https://gateway.test/") };
using var services = new ServiceCollection().AddSingleton(Options.Create(new ApiOptions())).BuildServiceProvider();
var context=new DefaultHttpContext { RequestServices=services };
context.Request.Path="/Desastres/Crear";
context.Request.Form=new FormCollection(new Dictionary<string,StringValues> { ["Key"]=Guid.NewGuid().ToString() });
var controller=new DesastresController(new DesastresClient(http)) {
    ControllerContext=new ControllerContext { HttpContext=context },
    ViewData=new ViewDataDictionary(new EmptyModelMetadataProvider(),new ModelStateDictionary()),
    TempData=new TempDataDictionary(context,new Temp())
};
var action=await controller.Dis04Guardar(new Dictionary<string,string?> {
    ["nombre"]="Ficha autorizada",["ubicacion"]="Neiva, Huila",["severidad"]="3",["descripcion"]="Registro",
    ["latitud"]="2.9",["longitud"]="-75.2",["disasterTypeId"]="1",["startedAt"]="2026-11-19T12:00:00Z"
});
if(action is not RedirectResult redirect || redirect.Url!="/Desastres/10")throw new Exception("Created response ID was not used for redirect");
context.Request.Path="/Desastres/10";
var result=(ViewResult)await controller.Dis03("10");
var model=(PantallaVm)result.Model!;
if(model.Detalle?["nombre"]!="Ficha autorizada" || model.Values["expectedVersion"]!="1")throw new Exception("Secondary denial erased the primary detail");
if(model.FlashError!=null || !model.SecondaryErrors.ContainsKey(0))throw new Exception("Secondary error was not isolated");
if(handler.Paths.IndexOf("/api/desastres/10")>=handler.Paths.IndexOf("/api/desastres/10/historial"))throw new Exception("History queried before primary detail");
Console.WriteLine("PASS unit: creation redirect, authorized primary detail/version and isolated history 403");

sealed class Responses : HttpMessageHandler
{
    public List<string> Paths=new();
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
    {
        var path=request.RequestUri!.AbsolutePath;Paths.Add(path);
        if(path.EndsWith("/historial"))return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var json=path=="/api/tipos-desastre"?"[{\"id\":1,\"name\":\"Terremoto\",\"isActive\":true}]":
            request.Method==HttpMethod.Get && path=="/api/desastres"?"[]":
            "{\"id\":10,\"nombre\":\"Ficha autorizada\",\"ubicacion\":\"Neiva, Huila\",\"descripcion\":\"Registro\",\"severidad\":3,\"latitud\":2.9,\"longitud\":-75.2,\"version\":1,\"phase\":\"Registered\"}";
        return Task.FromResult(new HttpResponseMessage(request.Method==HttpMethod.Post?HttpStatusCode.Created:HttpStatusCode.OK){Content=new StringContent(json,Encoding.UTF8,"application/json")});
    }
}
sealed class Temp : ITempDataProvider
{
    public IDictionary<string,object> LoadTempData(HttpContext context)=>new Dictionary<string,object>();
    public void SaveTempData(HttpContext context,IDictionary<string,object> values) { }
}
