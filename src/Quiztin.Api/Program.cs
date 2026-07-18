using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Quiztin.Modules.Assessment;
using Quiztin.Modules.Assessment.Infrastructure.Persistence;
using Quiztin.Modules.Assessment.Infrastructure.Seeding;
using Quiztin.Modules.Identity;
using Quiztin.Modules.Identity.Infrastructure.Persistence;
using Quiztin.Modules.Identity.Infrastructure.Seeding;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers come from the module assemblies (discovered via AddApplicationPart).
builder.Services.AddControllers()
    .AddApplicationPart(typeof(IdentityModule).Assembly)
    .AddApplicationPart(typeof(AssessmentModule).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Quiztin.Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

// Modules
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddAssessmentModule(builder.Configuration);

// ONE JWT validator for the whole app (was one per service). The Identity module
// issues the token; the host validates it. Issuer/Audience/Secret come from config
// (JwtSettings__Secret via env/user-secrets, never hardcoded — security.md §3).
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
            ValidAudience = jwtSettings.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.GetValue<string>("Secret")
                    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured (set JwtSettings__Secret).")))
        };
    });

var app = builder.Build();

// Modules apply their EF migrations at startup when enabled (env-gated, spec 0002).
// Populated when the module DbContexts land (Stage 3 migrates IdentityDbContext + QuizDbContext).
if (app.Configuration.GetValue<bool>("RUN_MIGRATIONS_ON_STARTUP"))
{
    // One migrate path for the whole app: each module owns its schema and its own
    // migration history in the single `quiztin` database (spec 0007).
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();
    migrationScope.ServiceProvider.GetRequiredService<QuizDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Seed the demo users first (Identity), then the classroom/quiz/enrolment that
    // reference them by Guid (Assessment). Assumes migrations have run.
    await IdentitySeeder.SeedDevelopmentAsync(app.Services);
    await DataSeeder.SeedDevelopmentDataAsync(app.Services);
}

// Serve the SPA assets baked into wwwroot (empty in local dev, where Vite serves the SPA).
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness for docker-compose, Railway health checks (spec 0002).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// The prerendered landing page, served only at the exact root path (spec 0003, AC-14):
// an explicit "/" endpoint outranks the SPA fallback, so a crawler or social scraper gets
// the landing markup + SEO tags, while every other route falls back to the neutral
// bootstrap. Falls back to index.html when the prerender file is absent (local dev).
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

// SPA client-side routing: any non-/api, non-asset, non-root path returns the neutral
// bootstrap index.html (never the prerendered landing markup, per AC-14).
app.MapFallbackToFile("index.html");

app.Run();
