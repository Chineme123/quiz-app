using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quiztin.Modules.Assessment.Api.BackgroundServices;
using Quiztin.Modules.Assessment.Application.Facades;
using Quiztin.Modules.Assessment.Application.Interfaces;
using Quiztin.Modules.Assessment.Application.Invokers;
using Quiztin.Modules.Assessment.Application.Services;
using Quiztin.Modules.Assessment.Domain.Events;
using Quiztin.Modules.Assessment.Domain.Factories;
using Quiztin.Modules.Assessment.Domain.Interfaces;
using Quiztin.Modules.Assessment.Domain.Strategies;
using Quiztin.Modules.Assessment.Infrastructure.Configuration;
using Quiztin.Modules.Assessment.Infrastructure.Factories;
using Quiztin.Modules.Assessment.Infrastructure.Feedback;
using Quiztin.Modules.Assessment.Infrastructure.Observers;
using Quiztin.Modules.Assessment.Infrastructure.Persistence;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;

namespace Quiztin.Modules.Assessment;

/// <summary>
/// Registration for the Quiz module (classrooms, quizzes, take-quiz, AI feedback).
/// The host calls AddAssessmentModule() and discovers this assembly's controllers via
/// AddApplicationPart. Lifted verbatim from the old QuizService Program.cs so the
/// in-process event/observer/strategy wiring and the AI feedback pipeline are unchanged.
/// </summary>
public static class AssessmentModule
{
    public static IServiceCollection AddAssessmentModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Database (all tables in the `quiz` schema; see QuizDbContext.HasDefaultSchema).
        services.AddDbContext<QuizDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null)));

        // DI Registrations
        services.AddScoped<IQuizRepository, QuizRepository>();
        services.AddScoped<IQuizAppService, QuizAppService>();
        services.AddScoped<IQuestionGenerationStrategy, StubLLMQuestionGenerationStrategy>();

        // UC2/UC3 Registrations (classroom create, join, and management; spec 0008)
        services.AddScoped<IClassroomRepository, ClassroomRepository>();
        services.AddScoped<IClassroomAppService, ClassroomAppService>();

        // UC8 Registrations
        services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
        services.AddScoped<IStrategyFactory, StrategyFactory>();
        services.AddScoped<QuizCommandInvoker>();
        services.AddScoped<TakeQuizFacade>();
        services.AddScoped<IEventDispatcher, Infrastructure.Events.EventDispatcher>();

        // Observers
        services.AddScoped<Domain.Observers.IObserver<QuizAttemptGradedEvent>, DashboardProjectionUpdater>();

        // AI feedback pipeline (spec 0005). The graded event enqueues the attempt; a hosted
        // worker generates feedback off the submit path so submitting stays fast (AC-1).
        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));
        services.Configure<FeedbackOptions>(configuration.GetSection(FeedbackOptions.SectionName));
        services.AddSingleton<IFeedbackQueue, FeedbackQueue>();
        services.AddScoped<Domain.Observers.IObserver<QuizAttemptGradedEvent>, FeedbackGenerationEnqueuer>();
        services.AddHostedService<FeedbackGenerationService>();

        // Feedback strategy: AI when the flag is on AND a key is present, else deterministic.
        // The flag lets the whole loop build and run before the Claude key is provisioned (AC-4).
        services.AddScoped<StandardFeedbackStrategy>();
        var aiFeedbackEnabled = configuration.GetValue<bool>("Feedback:AiEnabled");
        var anthropicKey = configuration.GetValue<string>("Anthropic:ApiKey");
        if (aiFeedbackEnabled && !string.IsNullOrWhiteSpace(anthropicKey))
        {
            // Pool the Anthropic HTTP client via IHttpClientFactory (the per-call timeout is
            // an HttpClient.Timeout, since Anthropic.SDK's call takes no CancellationToken).
            services.AddHttpClient<AiFeedbackStrategy>((sp, client) =>
            {
                var anthropicOptions = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, anthropicOptions.TimeoutSeconds));
            });
            services.AddScoped<IFeedbackStrategy>(sp => sp.GetRequiredService<AiFeedbackStrategy>());
        }
        else
        {
            services.AddScoped<IFeedbackStrategy>(sp => sp.GetRequiredService<StandardFeedbackStrategy>());
        }

        return services;
    }
}
