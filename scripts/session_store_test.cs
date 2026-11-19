#:project ../disaster-platform-web.csproj
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using disaster_platform_web.Security;

var directory = Path.Combine(Path.GetTempPath(), "razor-session-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["Security:DataProtectionPath"] = directory
    }).Build();
    SharedTicketStore Store() => new(configuration, DataProtectionProvider.Create(new DirectoryInfo(directory),
        builder => builder.SetApplicationName("session-store-regression")));
    AuthenticationTicket Ticket(DateTimeOffset expiry)
    {
        var properties = new AuthenticationProperties { ExpiresUtc = expiry };
        properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = "synthetic-private-token" } });
        return new(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "test-user") }, "Cookies")), properties, "Cookies");
    }
    var first = Store();
    var key = await first.StoreAsync(Ticket(DateTimeOffset.UtcNow.AddMinutes(5)));
    var second = Store();
    if ((await second.RetrieveAsync(key))?.Properties.GetTokenValue("access_token") != "synthetic-private-token") throw new Exception("Replica/restart lost the ticket");
    var file = Path.Combine(directory, "sessions", key + ".ticket");
    if (System.Text.Encoding.UTF8.GetString(await File.ReadAllBytesAsync(file)).Contains("synthetic-private-token")) throw new Exception("Plaintext token persisted");
    if (!OperatingSystem.IsWindows() && File.GetUnixFileMode(file) != (UnixFileMode.UserRead | UnixFileMode.UserWrite)) throw new Exception("Unsafe ticket permissions");
    await second.RemoveAsync(key);
    if (await first.RetrieveAsync(key) is not null) throw new Exception("Logout did not revoke the ticket across replicas");
    key = await first.StoreAsync(Ticket(DateTimeOffset.UtcNow.AddSeconds(-1)));
    if (await second.RetrieveAsync(key) is not null) throw new Exception("Expired ticket accepted");
    key = await first.StoreAsync(Ticket(DateTimeOffset.UtcNow.AddMinutes(5)));
    await File.WriteAllTextAsync(Path.Combine(directory, "sessions", key + ".ticket"), "tampered");
    if (await second.RetrieveAsync(key) is not null || await second.RetrieveAsync("../invalid") is not null) throw new Exception("Invalid ticket accepted");
    Console.WriteLine("PASS protected shared tickets: replica/restart, expiry, logout revocation, permissions and tamper rejection");
}
finally { Directory.Delete(directory, true); }
