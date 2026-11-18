var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
builder.Services.AddTransient<BearerHandler>();

if (builder.Configuration.GetValue<bool>("Api:UseMock"))
    builder.Services.AddSingleton<IDesastresClient, FakeDesastresClient>();
else
    builder.Services.AddHttpClient<IDesastresClient, DesastresClient>((sp, c) =>
        c.BaseAddress = new(sp.GetRequiredService<IOptions<ApiOptions>>().Value.GatewayUrl))
        .AddHttpMessageHandler<BearerHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.Run();
