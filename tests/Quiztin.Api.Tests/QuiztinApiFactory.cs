using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Quiztin.Api.Tests;

/// <summary>
/// Boots the real host pipeline in-process. Nothing here touches Postgres: migrations
/// stay off (RUN_MIGRATIONS_ON_STARTUP defaults false), the environment is "Testing" so
/// the Development-only seeders never run, and the DbContexts are only registered, never
/// opened. That keeps these routing tests fast and runnable without a database.
/// </summary>
public sealed class QuiztinApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Stand-in for the built SPA bundle, which only exists after a frontend build.</summary>
    public const string SpaShell = "<!doctype html><html><body><div id=\"root\"></div></body></html>";

    private readonly string _webRoot = Path.Combine(
        Path.GetTempPath(), $"quiztin-webroot-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // A real wwwroot/index.html, so the SPA fallback assertions exercise the actual
        // MapFallbackToFile path rather than passing vacuously on a missing file.
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), SpaShell);

        builder.UseEnvironment("Testing");
        builder.UseWebRoot(_webRoot);

        // The host throws on an empty signing key; production supplies this from the
        // environment (security.md §3). Test-only value, never a real secret.
        builder.UseSetting("JwtSettings:Secret", "quiztin-tests-signing-key-not-a-real-secret");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_webRoot)) Directory.Delete(_webRoot, recursive: true);
    }
}
