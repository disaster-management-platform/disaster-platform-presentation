using Microsoft.AspNetCore.Authentication;

namespace disaster_platform_web.Clients;

// Reenvía el access_token del usuario al Gateway / Integration Service.
public class BearerHandler(IHttpContextAccessor ctx) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
    {
        var http = ctx.HttpContext;
        if (http?.User.Identity?.IsAuthenticated == true &&
            await http.GetTokenAsync("access_token") is { } token)
            r.Headers.Authorization = new("Bearer", token);
        return await base.SendAsync(r, ct);
    }
}
