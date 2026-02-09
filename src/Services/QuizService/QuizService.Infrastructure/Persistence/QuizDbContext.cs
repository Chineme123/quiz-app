using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QuizService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace QuizService.Infrastructure.Persistence
{
    public class QuizDbContext : DbContext
    {
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Question> Questions { get; set; }

        public QuizDbContext(DbContextOptions<QuizDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Classroom>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
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
                entity.HasDiscriminator<string>("QuestionType")
                      .HasValue<MultipleChoiceQuestion>(nameof(MultipleChoiceQuestion))
                      .HasValue<TrueFalseQuestion>(nameof(TrueFalseQuestion))
                      .HasValue<ShortAnswerQuestion>(nameof(ShortAnswerQuestion));
            });

            modelBuilder.Entity<MultipleChoiceQuestion>(entity =>
            {
                // Store Options list as JSON string
                entity.Property(e => e.Options).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>()
                ).Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
            });
        }
    }
}
