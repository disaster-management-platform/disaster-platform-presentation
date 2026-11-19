using disaster_platform_web.Areas.Danos.Clients;
using disaster_platform_web.Areas.Desastres.Clients;
using disaster_platform_web.Areas.Organizaciones.Clients;
using disaster_platform_web.Areas.Personas.Clients;
using disaster_platform_web.Areas.Reconstruccion.Clients;
using disaster_platform_web.Clients;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();                       // solo la página de error
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
builder.Services.AddTransient<BearerHandler>();

// Home y Cuenta no dependen de ningún servicio
builder.Services.AddSingleton<IPantallaClient, FakePantallaClient>();

var mock = builder.Configuration.GetValue("Api:UseMock", true);

void Registrar<TI, TFake, TImpl>(Func<ApiOptions, string> url)
    where TI : class, IPantallaClient where TFake : class, TI where TImpl : class, TI
{
    if (mock) builder.Services.AddSingleton<TI, TFake>();
    else builder.Services.AddHttpClient<TI, TImpl>((sp, c) =>
    {
        c.BaseAddress = new Uri(url(sp.GetRequiredService<IOptions<ApiOptions>>().Value).TrimEnd('/') + "/");
        c.Timeout = TimeSpan.FromSeconds(5);
    }).AddHttpMessageHandler<BearerHandler>();
}

Registrar<IDesastresClient, FakeDesastresClient, DesastresClient>(o => o.GatewayUrl);
Registrar<IDanosClient, FakeDanosClient, DanosClient>(o => o.GatewayUrl);
Registrar<IReconstruccionClient, FakeReconstruccionClient, ReconstruccionClient>(o => o.GatewayUrl);
// Eden se consume solo a través del Integration Service
Registrar<IPersonasClient, FakePersonasClient, PersonasClient>(o => o.IntegrationUrl);
Registrar<IOrganizacionesClient, FakeOrganizacionesClient, OrganizacionesClient>(o => o.IntegrationUrl);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.Run();
