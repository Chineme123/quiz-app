using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuizService.Application.Interfaces;
using QuizService.Application.Services;
using QuizService.Application.Facades;
using QuizService.Application.Invokers;
using QuizService.Domain.Interfaces;
using QuizService.Domain.Factories;
using QuizService.Domain.Events;
using QuizService.Infrastructure.Persistence;
using QuizService.Infrastructure.Strategies;
using QuizService.Infrastructure.Factories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "QuizService.API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Database
builder.Services.AddDbContext<QuizDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null)));

// DI Registrations
builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IQuizAppService, QuizAppService>();
builder.Services.AddScoped<IQuestionGenerationStrategy, StubLLMQuestionGenerationStrategy>();

// UC8 Registrations
builder.Services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
builder.Services.AddScoped<IStrategyFactory, StrategyFactory>();
builder.Services.AddScoped<QuizCommandInvoker>();
builder.Services.AddScoped<TakeQuizFacade>();
builder.Services.AddScoped<IEventDispatcher, QuizService.Infrastructure.Events.EventDispatcher>();

// Observers
builder.Services.AddScoped<QuizService.Domain.Observers.IObserver<QuizService.Domain.Events.QuizAttemptGradedEvent>, QuizService.Infrastructure.Observers.DashboardProjectionUpdater>();

// Authentication — JWT settings come from configuration (env/user-secrets in
// practice; the signing key is never hardcoded — see security.md §3).
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Run Seeder
    await QuizService.Infrastructure.Seeding.DataSeeder.SeedDevelopmentDataAsync(app.Services);
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness for docker-compose, YARP health checks, and Railway (spec 0002).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
