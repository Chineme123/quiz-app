using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AuthUser>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(t => t.Id);

                // SHA-256 hex. Unique so a hash collision or a double insert fails loudly.
                entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
                entity.HasIndex(t => t.TokenHash).IsUnique();

                entity.HasIndex(t => t.UserId);
                entity.HasIndex(t => t.SessionId);

                entity.Property(t => t.CreatedAt).IsRequired();
                entity.Property(t => t.ExpiresAt).IsRequired();

                // Optimistic concurrency via Postgres' xmin system column (foundation §7 #18).
                // Two requests cannot rotate the same token: one wins, the loser reloads
                // and is judged by the grace rule rather than silently minting a second
                // live token in the family.
                entity.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
            });
        }
    }
}
