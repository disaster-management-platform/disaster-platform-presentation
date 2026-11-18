namespace disaster_platform_web.Clients;

public class BearerHandler(IHttpContextAccessor ctx) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
    {
        var t = await ctx.HttpContext!.GetTokenAsync("access_token"); // using Microsoft.AspNetCore.Authentication;
        if (t is not null) r.Headers.Authorization = new("Bearer", t);
        return await base.SendAsync(r, ct);
    }
}