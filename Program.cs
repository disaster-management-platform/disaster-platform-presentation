using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using System.Globalization;
using disaster_platform_web.Areas.Danos.Clients;
using disaster_platform_web.Areas.Desastres.Clients;
using disaster_platform_web.Areas.Organizaciones.Clients;
using disaster_platform_web.Areas.Personas.Clients;
using disaster_platform_web.Areas.Reconstruccion.Clients;
using disaster_platform_web.Areas.Recursos.Clients;
using disaster_platform_web.Areas.Sistema.Clients;
using disaster_platform_web.Clients;
using Microsoft.Extensions.Options;
using disaster_platform_web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();                       // solo la página de error
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
builder.Services.AddTransient<BearerHandler>();

var mock = builder.Configuration.GetValue("Api:UseMock", false);
if (mock && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Api:UseMock requires Development.");
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = mock ? CookieAuthenticationDefaults.AuthenticationScheme : OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.Cookie.Name = "platform-session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = false;
    options.LoginPath = "/Cuenta/Acceder";
});
if (!mock)
{
    var identity = builder.Configuration.GetSection("Security");
    if (!Uri.TryCreate(identity["Authority"], UriKind.Absolute, out var authority) || authority.Scheme != "https" ||
        string.IsNullOrWhiteSpace(identity["ClientSecret"]))
        throw new InvalidOperationException("Configure HTTPS Security:Authority and the OIDC client secret externally.");
    var keys = identity["DataProtectionPath"] ?? throw new InvalidOperationException("Configure a private shared Security:DataProtectionPath.");
    builder.Services.AddDataProtection().SetApplicationName("disaster-platform-presentation")
        .PersistKeysToFileSystem(new DirectoryInfo(keys));
    builder.Services.AddSingleton<SharedTicketStore>();
    builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
        .Configure<SharedTicketStore>((options, store) => options.SessionStore = store);
    builder.Services.AddAuthentication().AddOpenIdConnect(options =>
    {
        options.Authority = authority.ToString().TrimEnd('/');
        options.ClientId = identity["ClientId"] ?? "razor-web";
        options.ClientSecret = identity["ClientSecret"];
        options.ResponseType = "code";
        options.UsePkce = true;
        options.RequireHttpsMetadata = true;
        options.SaveTokens = true;
        options.RemoteAuthenticationTimeout = TimeSpan.FromMinutes(5);
        options.ProtocolValidator.NonceLifetime = options.RemoteAuthenticationTimeout;
        options.CorrelationCookie.Path = options.CallbackPath;
        options.NonceCookie.Path = options.CallbackPath;
        foreach (var scope in new[] { "disasters-api", "reconstruction-api", "damage-assessment-api", "integration-service" })
            options.Scope.Add(scope);
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = "role";
        options.Events.OnTicketReceived = context =>
        {
            var expiry = context.Properties?.GetTokenValue("expires_at");
            if (!DateTimeOffset.TryParse(expiry, CultureInfo.InvariantCulture, DateTimeStyles.None, out var expires) || expires <= DateTimeOffset.UtcNow)
                context.Fail("Missing or expired access token.");
            else
            {
                context.Properties!.ExpiresUtc = expires;
                context.Properties.StoreTokens(context.Properties.GetTokens().Where(token => token.Name != "refresh_token"));
            }
            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("El inicio de sesión no pudo completarse o expiró. Vuelve a /Cuenta/Acceder para intentarlo nuevamente.");
        };
    });
    builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
}
else builder.Services.AddAuthorization();

void Registrar<TI, TFake, TImpl>(Func<ApiOptions, string> url)
    where TI : class, IPantallaClient where TFake : class, TI where TImpl : class, TI
{
    if (mock) builder.Services.AddSingleton<TI, TFake>();
    else builder.Services.AddHttpClient<TI, TImpl>((sp, c) =>
    {
        c.BaseAddress = new Uri(url(sp.GetRequiredService<IOptions<ApiOptions>>().Value).TrimEnd('/') + "/");
        c.Timeout = TimeSpan.FromSeconds(30);
    }).AddHttpMessageHandler<BearerHandler>();
}

Registrar<IDesastresClient, FakeDesastresClient, DesastresClient>(o => o.GatewayUrl);
Registrar<IDanosClient, FakeDanosClient, DanosClient>(o => o.GatewayUrl);
Registrar<IReconstruccionClient, FakeReconstruccionClient, ReconstruccionClient>(o => o.GatewayUrl);
// Eden se consume solo a través del Integration Service
Registrar<IPersonasClient, FakePersonasClient, PersonasClient>(o => o.GatewayUrl);
Registrar<IOrganizacionesClient, FakeOrganizacionesClient, OrganizacionesClient>(o => o.GatewayUrl);
Registrar<IRecursosClient, FakeRecursosClient, RecursosClient>(o => o.GatewayUrl);
Registrar<ISistemaClient, FakeSistemaClient, SistemaClient>(o => o.GatewayUrl);

if (mock) builder.Services.AddSingleton<IPantallaClient, FakePantallaClient>();
else builder.Services.AddTransient<IPantallaClient>(sp => sp.GetRequiredService<ISistemaClient>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.Use(async (context, next) =>
{
    var chunks = context.Request.Cookies.Keys.Where(name => name.StartsWith("platform-sessionC", StringComparison.Ordinal)
        && int.TryParse(name["platform-sessionC".Length..], out _)).ToArray();
    if (chunks.Length != 0)
        foreach (var name in chunks.Append("platform-session"))
            context.Response.Cookies.Delete(name, new CookieOptions { Path = "/", Secure = true, HttpOnly = true });
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.Run();
