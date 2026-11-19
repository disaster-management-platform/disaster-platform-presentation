using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace disaster_platform_web.Controllers;

public class CuentaController(disaster_platform_web.Clients.IPantallaClient api, Microsoft.Extensions.Options.IOptions<disaster_platform_web.Clients.ApiOptions> options) : PantallaController(api)
{
    [AllowAnonymous, HttpGet("Cuenta/Acceder")]
    public async Task<IActionResult> Acceder(string? returnUrl = null)
    {
        if (options.Value.UseMock) return await Ver("AUTH");
        return Challenge(new AuthenticationProperties { RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/" }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost("Cuenta/Salir"), ValidateAntiForgeryToken]
    public IActionResult Salir() => options.Value.UseMock ? Redirect("/Cuenta/Acceder") :
        SignOut(new AuthenticationProperties { RedirectUri = "/" }, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
}
