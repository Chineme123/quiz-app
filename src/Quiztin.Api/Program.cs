using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Quiztin.Modules.Assessment;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers come from the module assemblies (discovered via AddApplicationPart).
builder.Services.AddControllers()
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
    using var migrationScope = app.Services.CreateScope();
    // Stage 3: migrationScope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();
    //          migrationScope.ServiceProvider.GetRequiredService<QuizDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness for docker-compose, Railway health checks (spec 0002).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
