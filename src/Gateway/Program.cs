// Quiztin gateway (spec 0002). The single origin: it forwards /api/* to the
// services and serves the SPA for everything else, so the in-memory access token
// and the HttpOnly refresh cookie (Path=/api/auth) work same-origin. The gateway
// only forwards credentials; each service stays authoritative for JWT validation
// (foundation §7 #30). Route + cluster config lives in appsettings.json and is
// overridable by environment in production (Railway private hostnames).

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Liveness for docker-compose, YARP active health checks, and Railway.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Serve the SPA assets baked into wwwroot in the gateway image. In local dev the SPA
// runs on the host Vite server and proxies /api here, so wwwroot is empty and this
// simply serves nothing.
app.UseStaticFiles();

// /api/* → the services (specific prefixes from config, never an /api catch-all).
app.MapReverseProxy();

// The prerendered landing page, served only at the exact root path (spec 0003,
// AC-14). An explicit endpoint outranks the SPA fallback below, so "/" gets the
// landing markup and its SEO tags for a crawler or a social card scraper, while
// every other route falls back to the neutral bootstrap and never sees the landing
// markup. Falls back to the neutral bootstrap when the prerender file is absent
// (local dev, where the SPA runs on Vite and wwwroot is empty).
app.MapGet("/", async (HttpContext context, IWebHostEnvironment env) =>
{
    var webRoot = env.WebRootPath ?? string.Empty;
    var landing = Path.Combine(webRoot, "index.prerender.html");
    var file = File.Exists(landing) ? landing : Path.Combine(webRoot, "index.html");
    if (!File.Exists(file))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(file);
});

// SPA client-side routing: any non-/api, non-asset, non-root path returns the
// neutral bootstrap index.html (never the prerendered landing markup, per AC-14).
app.MapFallbackToFile("index.html");

app.Run();
