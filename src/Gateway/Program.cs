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

// Serve the SPA (baked into wwwroot in the gateway image). In local dev the SPA
// runs on the host Vite server and proxies /api here, so wwwroot is empty and these
// simply serve nothing.
app.UseDefaultFiles();
app.UseStaticFiles();

// /api/* → the services (specific prefixes from config, never an /api catch-all).
app.MapReverseProxy();

// SPA client-side routing: any non-/api, non-asset path returns index.html.
app.MapFallbackToFile("index.html");

app.Run();
