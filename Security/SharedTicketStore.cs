using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace disaster_platform_web.Security;

public sealed class SharedTicketStore : ITicketStore
{
    private readonly string directory;
    private readonly IDataProtector protector;

    public SharedTicketStore(IConfiguration configuration, IDataProtectionProvider protection)
    {
        directory = configuration["Security:SessionStorePath"]
            ?? Path.Combine(configuration["Security:DataProtectionPath"]!, "sessions");
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        protector = protection.CreateProtector("Razor.AuthenticationTickets.v1");
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.ticket"))
            if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow) File.Delete(file);
        var key = Guid.NewGuid().ToString("N");
        await RenewAsync(key, ticket);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var path = TicketPath(key);
        var expiry = ticket.Properties.ExpiresUtc ?? throw new InvalidOperationException("Session expiration required.");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            await using (var stream = new FileStream(temporary, options))
                await stream.WriteAsync(protector.Protect(TicketSerializer.Default.Serialize(ticket)));
            File.SetLastWriteTimeUtc(temporary, expiry.UtcDateTime);
            File.Move(temporary, path, true);
        }
        finally { File.Delete(temporary); }
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!Guid.TryParseExact(key, "N", out _)) return null;
        try
        {
            var ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(await File.ReadAllBytesAsync(TicketPath(key))));
            if (ticket?.Properties.ExpiresUtc > DateTimeOffset.UtcNow) return ticket;
            await RemoveAsync(key);
        }
        catch (FileNotFoundException) { }
        catch (CryptographicException) { await RemoveAsync(key); }
        return null;
    }

    public Task RemoveAsync(string key)
    {
        if (Guid.TryParseExact(key, "N", out _)) File.Delete(TicketPath(key));
        return Task.CompletedTask;
    }

    private string TicketPath(string key) => Guid.TryParseExact(key, "N", out _)
        ? Path.Combine(directory, key + ".ticket") : throw new ArgumentException("Invalid session identifier.", nameof(key));
}
