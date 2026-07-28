using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Infrastructure.Persistence;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;

namespace Quiztin.Modules.Assessment.Infrastructure.Seeding
{
    /// <summary>
    /// Seeds the whole core loop precondition chain so a student can take a quiz and see
    /// AI feedback from a cold `docker compose up` (spec 0005, AC-11): a teacher, a
    /// classroom, a published quiz with a few questions, a student, and an enrolment.
    /// Development only, and idempotent, gated on fixed ids per entity.
    /// </summary>
    public static class DataSeeder
    {
        // Fixed identities so a developer can drive the flow with known ids. In a real
        // system these match seeded users in AuthService; the developer mints a JWT whose
        // NameIdentifier is SeedStudentId to take the quiz, or SeedTeacherId to author.
        public static readonly Guid SeedTeacherId = Guid.Parse("11111111-0000-0000-0000-000000000001");
        public static readonly Guid SeedStudentId = Guid.Parse("22222222-0000-0000-0000-000000000002");
        public static readonly Guid SeedClassroomId = Guid.Parse("33333333-0000-0000-0000-000000000003");
        public static readonly Guid SeedQuizId = Guid.Parse("44444444-0000-0000-0000-000000000004");

        // Two more students (spec 0010), so the teacher's results screens show real rows: Sam and
        // Alex take the seed quiz with different scores, Jordan is enrolled but has not taken it.
        // Ids match IdentitySeeder's SeedStudent2Id / SeedStudent3Id.
        public static readonly Guid SeedStudent2Id = Guid.Parse("22222222-0000-0000-0000-000000000005");
        public static readonly Guid SeedStudent3Id = Guid.Parse("22222222-0000-0000-0000-000000000006");

        // A second class and quiz (spec 0011), so the seed student's own results group by class:
        // Sam also finishes a quiz here, giving "My results" two classes with two standings.
        public static readonly Guid SeedClassroom2Id = Guid.Parse("33333333-0000-0000-0000-000000000007");
        public static readonly Guid SeedQuiz2Id = Guid.Parse("44444444-0000-0000-0000-000000000008");
        public const string SeedJoinCode2 = "SEED24";

        // Fixed join code for the seed classroom, so joining it in dev needs no DB lookup.
        // Uses only the unambiguous alphabet the generator draws from (spec 0008).
        public const string SeedJoinCode = "SEED23";

        public static async Task SeedDevelopmentDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<QuizDbContext>>();

            try
            {
                // Migration is the host's single job now (one migrate-on-startup, spec 0007).
                var seededAnything = false;

                // Classroom owned by the seed teacher.
                if (!await context.Classrooms.AnyAsync(c => c.Id == SeedClassroomId))
                {
                    // Pin the dev join code so a developer can always join the seed classroom
                    // with a known code, instead of reading a freshly generated one out of the DB.
                    context.Classrooms.Add(new Classroom(SeedTeacherId, "Seed Classroom (Dev)")
                    {
                        Id = SeedClassroomId,
                        JoinCode = SeedJoinCode
                    });
                    seededAnything = true;
                }

                // Enrol the seed student in that classroom (makes FR7 gating pass for them).
                if (!await context.Enrollments.AnyAsync(e => e.StudentId == SeedStudentId && e.ClassroomId == SeedClassroomId))
                {
                    context.Enrollments.Add(new Enrollment(SeedStudentId, SeedClassroomId));
                    seededAnything = true;
                }

                // A published quiz with one of each question type, so the take -> grade ->
                // feedback thread runs end to end. MaxAttempts is generous for dev iteration.
                if (!await context.Quizzes.AnyAsync(q => q.Id == SeedQuizId))
                {
                    var quiz = new Quiz(SeedClassroomId, "Networking Basics (Dev)", durationMinutes: 15, teacherId: SeedTeacherId)
                    {
                        Id = SeedQuizId,
                        IsPublished = true,
                        MaxAttempts = 5
                    };

                    quiz.Questions.Add(new MultipleChoiceQuestion(
                        "Which layer of the OSI model routes packets between networks?",
                        points: 1,
                        options: new List<string> { "The transport layer", "The network layer", "The data link layer", "The session layer" },
                        correctOptionIndex: 1)
                    { QuizId = SeedQuizId });

                    quiz.Questions.Add(new TrueFalseQuestion(
                        "TCP is a connectionless protocol.",
                        points: 1,
                        correctAnswer: false)
                    { QuizId = SeedQuizId });

                    quiz.Questions.Add(new ShortAnswerQuestion(
                        "What does DNS translate a domain name into?",
                        points: 1,
                        correctAnswerText: "IP address")
                    { QuizId = SeedQuizId });

                    context.Quizzes.Add(quiz);
                    seededAnything = true;
                }

                // Enrol the two extra students (spec 0010), so the roster the results count over
                // is more than one, and one of them (Jordan) never takes the quiz.
                foreach (var studentId in new[] { SeedStudent2Id, SeedStudent3Id })
                {
                    if (!await context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.ClassroomId == SeedClassroomId))
                    {
                        context.Enrollments.Add(new Enrollment(studentId, SeedClassroomId));
                        seededAnything = true;
                    }
                }

                // A second classroom with its own published quiz (spec 0011), so the seed student's
                // own results screen shows more than one class. The seed student is enrolled here too.
                if (!await context.Classrooms.AnyAsync(c => c.Id == SeedClassroom2Id))
                {
                    context.Classrooms.Add(new Classroom(SeedTeacherId, "Study Group (Dev)")
                    {
                        Id = SeedClassroom2Id,
                        JoinCode = SeedJoinCode2
                    });
                    seededAnything = true;
                }

                if (!await context.Enrollments.AnyAsync(e => e.StudentId == SeedStudentId && e.ClassroomId == SeedClassroom2Id))
                {
                    context.Enrollments.Add(new Enrollment(SeedStudentId, SeedClassroom2Id));
                    seededAnything = true;
                }

                if (!await context.Quizzes.AnyAsync(q => q.Id == SeedQuiz2Id))
                {
                    var quiz2 = new Quiz(SeedClassroom2Id, "Study Skills (Dev)", durationMinutes: 10, teacherId: SeedTeacherId)
                    {
                        Id = SeedQuiz2Id,
                        IsPublished = true,
                        MaxAttempts = 5
                    };

                    quiz2.Questions.Add(new TrueFalseQuestion(
                        "Spacing your study sessions out over time beats cramming.",
                        points: 1,
                        correctAnswer: true)
                    { QuizId = SeedQuiz2Id });

                    quiz2.Questions.Add(new ShortAnswerQuestion(
                        "What technique tests yourself to strengthen memory?",
                        points: 1,
                        correctAnswerText: "Active recall")
                    { QuizId = SeedQuiz2Id });

                    context.Quizzes.Add(quiz2);
                    seededAnything = true;
                }

                if (seededAnything)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation(
                        "Seeded the core loop: classroom {ClassroomId}, quiz {QuizId}, student {StudentId} enrolled.",
                        SeedClassroomId, SeedQuizId, SeedStudentId);
                }
                else
                {
                    logger.LogInformation("Core loop seed data already present. Skipping.");
                }

                // Finished attempts, seeded only after the quizzes are persisted above: they load
                // each quiz with its questions from the database, so this must follow the save (a
                // quiz is merely tracked, not yet in the DB, until then). On the first quiz Sam got
                // two of three and Alex all three; Jordan has no attempt (he reads as "Not taken").
                // Sam also finishes the second class's quiz (spec 0011), so his own results group by
                // class. Idempotent per (quiz, student).
                var attemptsSeeded = false;

                var seedQuiz = await context.Quizzes.Include(q => q.Questions)
                    .FirstOrDefaultAsync(q => q.Id == SeedQuizId);
                if (seedQuiz != null)
                {
                    var questions = seedQuiz.Questions.ToList();
                    if (await EnsureGradedAttemptAsync(context, SeedQuizId, SeedStudentId, questions, correctCount: 2)) attemptsSeeded = true;
                    if (await EnsureGradedAttemptAsync(context, SeedQuizId, SeedStudent2Id, questions, correctCount: 3)) attemptsSeeded = true;
                }

                var seedQuiz2 = await context.Quizzes.Include(q => q.Questions)
                    .FirstOrDefaultAsync(q => q.Id == SeedQuiz2Id);
                if (seedQuiz2 != null)
                {
                    var questions2 = seedQuiz2.Questions.ToList();
                    if (await EnsureGradedAttemptAsync(context, SeedQuiz2Id, SeedStudentId, questions2, correctCount: 1)) attemptsSeeded = true;
                }

                if (attemptsSeeded)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded finished attempts for the results screens (spec 0010, spec 0011).");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        /// <summary>
        /// Adds one submitted, graded attempt for a student if they have none on the quiz yet.
        /// Drives the real attempt lifecycle (start, save, submit, evaluate), so a seeded row is
        /// exactly what a real take produces, answers and score alike. Idempotent.
        /// </summary>
        private static async Task<bool> EnsureGradedAttemptAsync(
            QuizDbContext context, Guid quizId, Guid studentId, IReadOnlyList<Question> questions, int correctCount)
        {
            if (await context.QuizAttempts.AnyAsync(a => a.QuizId == quizId && a.StudentId == studentId))
                return false;

            var answers = new Dictionary<Guid, string>();
            for (var i = 0; i < questions.Count; i++)
                answers[questions[i].Id] = AnswerFor(questions[i], correct: i < correctCount);

            var attempt = new QuizAttempt(quizId, studentId);
            attempt.Start(durationMinutes: 15);
            attempt.SaveDraftAnswers(answers, DateTime.UtcNow);
            attempt.Submit();
            attempt.Evaluate(new PointsScoringStrategy(), questions);

            context.QuizAttempts.Add(attempt);
            return true;
        }

        /// <summary>The submitted-answer string a question expects (an option index, "true"/"false",
        /// or the text), correct or deliberately wrong, so a seeded attempt scores predictably.</summary>
        private static string AnswerFor(Question question, bool correct) => question switch
        {
            MultipleChoiceQuestion mc => correct
                ? mc.CorrectOptionIndex.ToString()
                : ((mc.CorrectOptionIndex + 1) % Math.Max(mc.Options.Count, 1)).ToString(),
            TrueFalseQuestion tf => (correct ? tf.CorrectAnswer : !tf.CorrectAnswer) ? "true" : "false",
            ShortAnswerQuestion sa => correct ? sa.CorrectAnswerText : "Not sure",
            _ => string.Empty
        };
    }
}
