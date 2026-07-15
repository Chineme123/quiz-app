using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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
using QuizService.Infrastructure.Feedback;
using QuizService.Infrastructure.Configuration;
using QuizService.Infrastructure.Observers;
using QuizService.Domain.Strategies;
using QuizService.API.BackgroundServices;
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
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
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

// AI feedback pipeline (spec 0005). The graded event enqueues the attempt; a hosted
// worker generates feedback off the submit path so submitting stays fast (AC-1).
builder.Services.Configure<AnthropicOptions>(builder.Configuration.GetSection(AnthropicOptions.SectionName));
builder.Services.Configure<FeedbackOptions>(builder.Configuration.GetSection(FeedbackOptions.SectionName));
builder.Services.AddSingleton<IFeedbackQueue, FeedbackQueue>();
builder.Services.AddScoped<QuizService.Domain.Observers.IObserver<QuizService.Domain.Events.QuizAttemptGradedEvent>, FeedbackGenerationEnqueuer>();
builder.Services.AddHostedService<FeedbackGenerationService>();

// Feedback strategy: AI when the flag is on AND a key is present, else deterministic.
// The flag lets the whole loop build and run before the Claude key is provisioned (AC-4).
builder.Services.AddScoped<StandardFeedbackStrategy>();
var aiFeedbackEnabled = builder.Configuration.GetValue<bool>("Feedback:AiEnabled");
var anthropicKey = builder.Configuration.GetValue<string>("Anthropic:ApiKey");
if (aiFeedbackEnabled && !string.IsNullOrWhiteSpace(anthropicKey))
{
    builder.Services.AddScoped<IFeedbackStrategy, AiFeedbackStrategy>();
}
else
{
    builder.Services.AddScoped<IFeedbackStrategy>(sp => sp.GetRequiredService<StandardFeedbackStrategy>());
}

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

// Apply EF migrations at startup when enabled (spec 0002). On in production (Railway)
// and in docker-compose so the databases have their tables; off by default otherwise.
// The Development seeder below still handles demo data.
if (app.Configuration.GetValue<bool>("RUN_MIGRATIONS_ON_STARTUP"))
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<QuizDbContext>().Database.Migrate();
}

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
