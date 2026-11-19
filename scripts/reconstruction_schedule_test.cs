#:project ../disaster-platform-web.csproj
#:property JsonSerializerIsReflectionEnabledByDefault=true
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using disaster_platform_web.Areas.Reconstruccion.Clients;
using disaster_platform_web.Screens;

using var http = new HttpClient(new Responses()) { BaseAddress = new Uri("https://gateway.test/") };
var client = new ReconstruccionClient(http);
var query = new Dictionary<string,string?> { ["disasterId"] = "4" };
PantallaVm Model() => new() { Screen = Catalog.Get("REC-06"), RouteId = Guid.Empty.ToString() };
var model = Model();
await client.PrepararAsync(model, query, default);
if (model.Values["activities"] != "DEMO Inspección;Equipo demo;2026-11-19;2026-11-25;400000")
    throw new Exception("Stored activities were not mapped to the schedule editor contract");
model = Model();
model.Values["activities"] = "Edición en curso;Responsable;2026-12-01;2026-12-02;1.00";
await client.PrepararAsync(model, query, default);
if (model.Values["activities"] != "Edición en curso;Responsable;2026-12-01;2026-12-02;1.00")
    throw new Exception("Unsubmitted schedule edits were overwritten");
Console.WriteLine("PASS persisted schedule mapping and preservation of unsubmitted edits");

sealed class Responses : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var path = request.RequestUri!.AbsolutePath;
        var json = path == "/api/desastres" ? "[]" : path.EndsWith("/financiacion")
            ? """{"source":"DEMO","sourceId":"demo-source","availableAmount":1000000}"""
            : """{"disasterId":4,"status":"Draft","version":1,"activities":[{"name":"DEMO Inspección","responsible":"Equipo demo","startsAt":"2026-11-19T00:00:00Z","dueAt":"2026-11-25T00:00:00Z","budget":400000}]}""";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
    }
}
