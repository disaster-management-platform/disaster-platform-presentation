using disaster_platform_web.Screens;

var builder = WebApplication.CreateBuilder(args);

Catalog.EdenBase = builder.Configuration["EdenBaseUrl"] ?? Catalog.EdenBase;

// Todas las pantallas del catálogo (menos HOME, que es "/") se atienden con la página Pages/Screen.cshtml
builder.Services.AddRazorPages(options =>
{
    foreach (var s in Catalog.All.Where(s => s.Route.Length > 0))
        options.Conventions.AddPageRoute("/Screen", s.Route);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();