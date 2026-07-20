using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Quiztin.Modules.Assessment.Infrastructure.Persistence
{
    public class QuizDbContext : DbContext
    {
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizAnswer> QuizAnswers { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<ProcessedCommand> ProcessedCommands { get; set; }

        public QuizDbContext(DbContextOptions<QuizDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // All Quiz-module tables live in the `quiz` Postgres schema (modular-monolith
            // module boundary, spec 0007). Cross-module references (TeacherId, StudentId)
            // stay plain indexed Guids with no cross-schema FK, so the module stays splittable.
            modelBuilder.HasDefaultSchema("quiz");

            modelBuilder.Entity<Classroom>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

                // The join code is the capability to find a class, so it must be unique across
                // every classroom, archived ones included: archiving never frees a code for
                // reuse. The unique index is also what makes regenerate-on-collision safe,
                // rather than relying on a read-then-write check (spec 0008).
                entity.Property(e => e.JoinCode).IsRequired().HasMaxLength(JoinCodeGenerator.Length);
                entity.HasIndex(e => e.JoinCode).IsUnique();
            });

            modelBuilder.Entity<Quiz>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.HasOne<Classroom>()
                      .WithMany(c => c.Quizzes)
                      .HasForeignKey(q => q.ClassroomId);
                
                entity.HasMany(q => q.Questions)
                      .WithOne()
                      .HasForeignKey(q => q.QuizId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.Id);
                // The Id is a client generated Guid (set in the constructor). Without this, EF
                // treats the key as store generated, so a new question discovered in a tracked
                // quiz's Questions collection (AddQuestion, Generate) is assumed to be an
                // existing row and UPDATEd (0 rows) instead of INSERTed, throwing a phantom
                // concurrency conflict against a real database. Same fix, and same reason, as
                // QuizAnswer.Id below.
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasDiscriminator<string>("QuestionType")
                      .HasValue<MultipleChoiceQuestion>(nameof(MultipleChoiceQuestion))
                      .HasValue<TrueFalseQuestion>(nameof(TrueFalseQuestion))
                      .HasValue<ShortAnswerQuestion>(nameof(ShortAnswerQuestion));
            });

            modelBuilder.Entity<MultipleChoiceQuestion>(entity =>
            {
                // Store Options list as JSON string
                entity.Property(e => e.Options).HasColumnType("jsonb").HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>()
                ).Metadata.SetValueComparer(
                    new ValueComparer<List<string>>(
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
            });

            modelBuilder.Entity<QuizAttempt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne<Quiz>()
                      .WithMany()
                      .HasForeignKey(a => a.QuizId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(a => a.Answers)
                      .WithOne()
                      .HasForeignKey(ans => ans.QuizAttemptId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.Ignore("_currentState");
                entity.Ignore("_answers");

                // Store the feedback status as text, matching the CurrentStateName style,
                // so the column reads plainly in the DB (spec 0005).
                entity.Property(a => a.FeedbackStatus).HasConversion<string>();

                // Null unless the attempt is Abandoned; text for the same reason as above
                // (spec 0006). The attempt limit reads it: only Superseded costs an attempt.
                entity.Property(a => a.AbandonReason).HasConversion<string>();

                // The in progress answers as one jsonb column, replaced whole on every save
                // (spec 0006). Mapped from the private field so the entity stays encapsulated,
                // and configured explicitly because EF discovers the field either way and
                // cannot map a Dictionary on its own. Follows the Options jsonb precedent above.
                var draftAnswers = entity.Property<Dictionary<Guid, string>>("_draftAnswers");
                draftAnswers
                    .HasColumnName("DraftAnswers")
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<Guid, string>>(v, (JsonSerializerOptions)null)
                             ?? new Dictionary<Guid, string>());
                draftAnswers.Metadata.SetValueComparer(
                    new ValueComparer<Dictionary<Guid, string>>(
                        (d1, d2) => d1.Count == d2.Count && !d1.Except(d2).Any(),
                        d => d.Aggregate(0, (a, kv) => HashCode.Combine(a, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
                        d => new Dictionary<Guid, string>(d)));

                // Optimistic concurrency via Postgres' xmin system column.
                entity.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
            });

            modelBuilder.Entity<QuizAnswer>(entity =>
            {
                entity.HasKey(e => e.Id);
                // The Id is a client generated Guid (set in the constructor). Without this,
                // EF treats the key as store generated, so a new answer discovered in a
                // tracked attempt's collection at submit is assumed to be an existing row
                // and UPDATEd (0 rows) instead of INSERTed. Marking it client generated
                // makes EF add new answers correctly.
                entity.Property(e => e.Id).ValueGeneratedNever();
                // Nullable until feedback is written, then "Ai" or "Deterministic" as text.
                entity.Property(a => a.FeedbackSource).HasConversion<string>();
            });

            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Already present: at most one enrolment per user per class. This is what makes
                // join idempotent at the database layer, so a double submit or two racing
                // requests can never produce a second row (spec 0008).
                entity.HasIndex(e => new { e.StudentId, e.ClassroomId }).IsUnique();

                // ClassroomId becomes a real FK: it is a within-module reference, so integrity
                // is enforced by the database. StudentId stays a plain Guid because users live
                // in the Identity module across a schema boundary (spec 0007).
                entity.HasOne<Classroom>()
                      .WithMany()
                      .HasForeignKey(e => e.ClassroomId);
            });
        }
    }
}
