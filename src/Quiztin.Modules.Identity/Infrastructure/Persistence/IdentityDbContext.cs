using Quiztin.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Quiztin.Modules.Identity.Infrastructure.Persistence
{
    public class IdentityDbContext : DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

        public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Profile> Profiles => Set<Profile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // All Identity-module tables live in the `identity` Postgres schema (spec 0007).
            modelBuilder.HasDefaultSchema("identity");

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

            modelBuilder.Entity<Profile>(entity =>
            {
                // Shared PK/FK: Profile.UserId is the primary key AND the foreign key to
                // the canonical user (identity.AuthUsers.Id). This merge fixes the old
                // cross-database FK gap — registration creates the AuthUser row, so a
                // first-time profile save now has a real parent row to reference (spec 0007).
                entity.HasKey(e => e.UserId);
                entity.HasOne(p => p.User)
                      .WithOne()
                      .HasForeignKey<Profile>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Bio).HasMaxLength(500);
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.Property(e => e.School).HasMaxLength(200);
                entity.Property(e => e.Department).HasMaxLength(200);
                entity.Property(e => e.AcademicLevel).HasMaxLength(50);
                entity.Property(e => e.InstructorType).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
            });
        }
    }
}
