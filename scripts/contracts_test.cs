#:project ../disaster-platform-web.csproj
#:property JsonSerializerIsReflectionEnabledByDefault=true
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using disaster_platform_web.Clients;
using disaster_platform_web.Screens;
using disaster_platform_web.Areas.Danos.Clients;
using disaster_platform_web.Areas.Organizaciones.Clients;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
Environment.SetEnvironmentVariable("TZ", "America/Bogota");
TimeZoneInfo.ClearCachedData();
var original = DateTimeOffset.Parse("2026-11-18T12:34:56.123456Z");
var values = new Dictionary<string, string?> {
    ["startedAt"] = "2026-11-18T07:34:56.123",
    ["original_startedAt"] = original.ToString("O"),
    ["display_startedAt"] = "2026-11-18T07:34:56.123"
};
var date = typeof(HttpPantallaClient).GetMethod("Date", BindingFlags.Static | BindingFlags.NonPublic)!;
Check((DateTimeOffset)date.Invoke(null, [values, "startedAt"])! == original, "Unchanged date lost microseconds");
values["startedAt"] = "2026-11-18T07:34:57.124";
Check((DateTimeOffset)date.Invoke(null, [values, "startedAt"])! == DateTimeOffset.Parse("2026-11-18T12:34:57.124Z"), "Changed date was ignored");
Console.WriteLine("PASS unchanged date precision and intentional date edit");

var handler = new CaptureHandler();
using var http = new HttpClient(handler) { BaseAddress = new Uri("http://gateway.test/") };
var client = new DanosClient(http);
var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\nSynthetic stream regression\n%%EOF\n");
using var shared = new MemoryStream(bytes);
var file = new FormFile(shared, 0, bytes.Length, "Values[evidence]", "synthetic.pdf") {
    Headers = new HeaderDictionary(), ContentType = "application/pdf"
};
var upload = typeof(HttpPantallaClient).GetMethod("Upload", BindingFlags.Instance | BindingFlags.NonPublic)!;
var result = (Task<JsonNode?>)upload.Invoke(client, ["api/evaluaciones/unit/evidencias", file, "", Guid.NewGuid(), true, CancellationToken.None])!;
await result;
var body = Encoding.UTF8.GetString(handler.Body);
Check(body.Contains(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()), "Multipart SHA-256 missing");
Check(body.Contains(Encoding.ASCII.GetString(bytes)), "Shared file stream was corrupted before upload");
Console.WriteLine("PASS multipart bytes and SHA-256 with shared FormFile stream");

var organizations = new OrganizacionesClient(http);
var vm = new PantallaVm { Screen = Catalog.Get("ORG-01") };
await organizations.PrepararAsync(vm, new Dictionary<string,string?>(), CancellationToken.None);
var rows = await organizations.FilasAsync(vm.Screen, [new("id", "ID"), new("name", "Name")], null, null, -1, CancellationToken.None);
Check(rows?.Count == 1 && rows[0].Cells[1].Text == "Scoped organization", "Organization envelope not mapped");
Console.WriteLine("PASS organization directory independence and real-envelope projection unit contract");

sealed class CaptureHandler : HttpMessageHandler
{
    public byte[] Body { get; private set; } = [];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.RequestUri!.AbsolutePath == "/api/desastres") throw new Exception("Directory must not depend on Disaster availability");
        if (request.Content != null) Body = await request.Content.ReadAsByteArrayAsync(ct);
        var json = request.Method == HttpMethod.Get
            ? "{\"items\":[{\"id\":\"3\",\"name\":\"Scoped organization\"}],\"hasMore\":false}"
            : "{\"status\":\"Verified\"}";
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
