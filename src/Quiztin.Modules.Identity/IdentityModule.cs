using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quiztin.Modules.Identity.Application.Configuration;
using Quiztin.Modules.Identity.Application.Interfaces;
using Quiztin.Modules.Identity.Application.Services;
using Quiztin.Modules.Identity.Application.Strategies;
using Quiztin.Modules.Identity.Domain.Interfaces;
using Quiztin.Modules.Identity.Infrastructure.Persistence;
using Quiztin.Modules.Identity.Infrastructure.Security;

namespace Quiztin.Modules.Identity;

/// <summary>
/// Registration for the Identity module (authentication, JWT issuance, refresh-token
/// rotation, and user profiles). Merges the old AuthService and UserService: one
/// IdentityDbContext, one canonical users table (AuthUser), and Profile 1:1 to it.
/// The host validates the JWT once; this module issues it.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Database (all Identity tables in the `identity` schema; see IdentityDbContext).
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null)));

        // Token lifetimes + refresh cookie flags (Cookie:Secure is false only for local http).
        var authTokenOptions = configuration.GetSection("AuthTokens").Get<AuthTokenOptions>() ?? new AuthTokenOptions();
        services.AddSingleton(authTokenOptions);
        services.AddSingleton(TimeProvider.System);

        // Auth wiring (issues the JWT; the host validates it).
        services.AddScoped<IAuthUserRepository, AuthUserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuthService, AuthAppService>();

        // Profile wiring (role-aware update strategies resolved as IEnumerable by the controller).
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IProfileUpdateStrategy, StudentProfileUpdateStrategy>();
        services.AddScoped<IProfileUpdateStrategy, TeacherProfileUpdateStrategy>();

        return services;
    }
}
