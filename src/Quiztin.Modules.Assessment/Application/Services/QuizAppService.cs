using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Interfaces;
using Quiztin.Modules.Assessment.Application.Results;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Factories;
using Quiztin.Modules.Assessment.Domain.Interfaces;

namespace Quiztin.Modules.Assessment.Application.Services
{
    public class QuizAppService : IQuizAppService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IQuizAttemptRepository _attemptRepository;
        private readonly IEnumerable<IQuestionGenerationStrategy> _strategies;

        public QuizAppService(
            IQuizRepository quizRepository,
            IQuizAttemptRepository attemptRepository,
            IEnumerable<IQuestionGenerationStrategy> strategies)
        {
            _quizRepository = quizRepository;
            _attemptRepository = attemptRepository;
            _strategies = strategies;
        }

        public async Task<AuthoringResult> CreateQuizAsync(Guid classroomId, Guid teacherId, CreateQuizDto input)
        {
            var classroom = await _quizRepository.GetClassroomAsync(classroomId);
            // Not found and not yours read the same: 404, so a classroom's existence never leaks (AC-3).
            if (classroom == null || classroom.TeacherId != teacherId)
                return AuthoringResult.NotFound();

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(input.Title))
                errors.Add("A quiz needs a title.");
            if (input.DurationMinutes <= 0)
                errors.Add("A quiz's duration must be greater than zero minutes.");
            if (errors.Count > 0)
                return AuthoringResult.Invalid(errors);

            var quiz = new Quiz(classroomId, input.Title, input.DurationMinutes, teacherId);
            await _quizRepository.AddAsync(quiz);

            // A brand new quiz has no attempts, so it is never locked.
            return AuthoringResult.Ok(MapToDto(quiz, isLocked: false));
        }

        public async Task<AuthoringResult> AddQuestionAsync(Guid quizId, Guid teacherId, AddQuestionDto input)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null || quiz.CreatedByTeacherId != teacherId)
                return AuthoringResult.NotFound();

            if (await _attemptRepository.HasAnyAttemptAsync(quizId))
                return AuthoringResult.Locked();

            var built = BuildQuestion(input);
            if (built.Question is not { } question)
                return AuthoringResult.Invalid(built.Errors);

            question.QuizId = quiz.Id;
            quiz.Questions.Add(question);
            await _quizRepository.UpdateAsync(quiz);

            return AuthoringResult.Ok(MapToDto(quiz, isLocked: false));
        }

        public async Task<AuthoringResult> EditQuestionAsync(Guid quizId, Guid questionId, Guid teacherId, AddQuestionDto input)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null || quiz.CreatedByTeacherId != teacherId)
                return AuthoringResult.NotFound();

            var existing = quiz.Questions.FirstOrDefault(q => q.Id == questionId);
            if (existing == null)
                return AuthoringResult.NotFound();

            if (await _attemptRepository.HasAnyAttemptAsync(quizId))
                return AuthoringResult.Locked();

            var built = BuildQuestion(input);
            if (built.Question is not { } candidate)
                return AuthoringResult.Invalid(built.Errors);

            // A question's type is fixed once created: it is a different row in the table per
            // hierarchy mapping, so changing type is a delete then add, not an in place edit.
            if (candidate.GetType() != existing.GetType())
                return AuthoringResult.Invalid(
                    "A question's type cannot change on edit. Delete this question and add a new one of the type you want.");

            ApplyEditedContent(existing, candidate);
            await _quizRepository.UpdateAsync(quiz);

            return AuthoringResult.Ok(MapToDto(quiz, isLocked: false));
        }

        public async Task<AuthoringResult> DeleteQuestionAsync(Guid quizId, Guid questionId, Guid teacherId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null || quiz.CreatedByTeacherId != teacherId)
                return AuthoringResult.NotFound();

            var existing = quiz.Questions.FirstOrDefault(q => q.Id == questionId);
            if (existing == null)
                return AuthoringResult.NotFound();

            if (await _attemptRepository.HasAnyAttemptAsync(quizId))
                return AuthoringResult.Locked();

            // Removing from the tracked collection orphans the question; the Quiz to Questions
            // relationship cascades on delete, so the save issues the DELETE. UpdateAsync is the
            // bare SaveChanges (no DbSet.Update), the same discipline the add path relies on.
            quiz.Questions.Remove(existing);
            await _quizRepository.UpdateAsync(quiz);

            return AuthoringResult.Deleted();
        }

        public async Task<QuizDto?> GetQuizForEditingAsync(Guid quizId, Guid teacherId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            // Owner scoped: a non owner and a missing quiz both return null, so the controller
            // answers 404 either way and a quiz's existence never leaks (AC-10).
            if (quiz == null || quiz.CreatedByTeacherId != teacherId)
                return null;

            var isLocked = await _attemptRepository.HasAnyAttemptAsync(quizId);
            return MapToDto(quiz, isLocked);
        }

        public async Task<IReadOnlyList<QuizSummaryDto>?> GetQuizzesForClassroomAsync(Guid classroomId, Guid teacherId)
        {
            var classroom = await _quizRepository.GetClassroomAsync(classroomId);
            if (classroom == null || classroom.TeacherId != teacherId)
                return null;

            var quizzes = await _quizRepository.GetByClassroomAsync(classroomId);
            var attemptCounts = await _attemptRepository.GetAttemptCountsByQuizAsync(
                quizzes.Select(q => q.Id).ToList());

            return quizzes.Select(q => new QuizSummaryDto
            {
                Id = q.Id,
                Title = q.Title,
                IsPublished = q.IsPublished,
                QuestionCount = q.Questions.Count,
                AttemptCount = attemptCounts.TryGetValue(q.Id, out var count) ? count : 0
            }).ToList();
        }

        public async Task<QuizDto> GenerateQuestionsAsync(Guid quizId, Guid teacherId, GenerateQuestionsDto input)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null) throw new KeyNotFoundException("Quiz not found.");
            if (quiz.CreatedByTeacherId != teacherId) throw new UnauthorizedAccessException("Not authorized.");

            var strategy = _strategies.FirstOrDefault(s => s.ModeName.Equals(input.Mode, StringComparison.OrdinalIgnoreCase));
            if (strategy == null)
            {
                throw new ArgumentException($"Strategy '{input.Mode}' not found.");
            }

            var generatedQuestions = await strategy.GenerateQuestionsAsync(input.Topic, input.Count, input.Difficulty);

            foreach (var q in generatedQuestions)
            {
                q.QuizId = quizId;
                quiz.Questions.Add(q);
            }

            await _quizRepository.UpdateAsync(quiz);
            // Task 3 replaces this stub append with the real draft flow (and gates it on the lock);
            // for now the echo still reports the honest lock state.
            var isLocked = await _attemptRepository.HasAnyAttemptAsync(quizId);
            return MapToDto(quiz, isLocked);
        }

        public async Task<PublishResult> PublishAsync(Guid quizId, Guid teacherId, PublishQuizDto input)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            // Not found and not yours read the same: 404, so a quiz's existence never leaks (AC-1).
            if (quiz == null || quiz.CreatedByTeacherId != teacherId)
                return PublishResult.Failed(PublishOutcome.NotFound);

            // An empty published quiz would show a student a quiz with nothing to answer.
            if (quiz.Questions.Count == 0)
                return PublishResult.Failed(PublishOutcome.NoQuestions);

            // A window with both bounds must run forward.
            if (input.AvailableFrom.HasValue && input.AvailableTo.HasValue
                && input.AvailableFrom.Value >= input.AvailableTo.Value)
                return PublishResult.Failed(PublishOutcome.InvalidWindow);

            if (input.MaxAttempts < 1)
                return PublishResult.Failed(PublishOutcome.InvalidMaxAttempts);

            quiz.AvailableFrom = input.AvailableFrom;
            quiz.AvailableTo = input.AvailableTo;
            quiz.MaxAttempts = input.MaxAttempts;
            quiz.IsPublished = true;
            await _quizRepository.UpdateAsync(quiz);

            // Publish is allowed on a quiz that already has attempts (AC-9), so report the real
            // lock state rather than assuming it is unlocked.
            return PublishResult.Ok(MapToDto(quiz, await _attemptRepository.HasAnyAttemptAsync(quiz.Id)));
        }

        public async Task<PublishResult> UnpublishAsync(Guid quizId, Guid teacherId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null || quiz.CreatedByTeacherId != teacherId)
                return PublishResult.Failed(PublishOutcome.NotFound);

            quiz.IsPublished = false;
            await _quizRepository.UpdateAsync(quiz);

            return PublishResult.Ok(MapToDto(quiz, await _attemptRepository.HasAnyAttemptAsync(quiz.Id)));
        }

        // One validation and construction path for a question, shared by add, edit, and (via the
        // strategy) generation, so the three cannot drift on what a valid question is (spec 0009).
        private static QuestionCreationResult BuildQuestion(AddQuestionDto input) =>
            input.QuestionType switch
            {
                "MultipleChoice" => QuestionFactory.TryCreateMultipleChoice(input.Prompt, input.Points, input.Options, input.CorrectOptionIndex),
                "TrueFalse" => QuestionFactory.TryCreateTrueFalse(input.Prompt, input.Points, input.CorrectAnswerBool),
                "ShortAnswer" => QuestionFactory.TryCreateShortAnswer(input.Prompt, input.Points, input.CorrectAnswerText),
                _ => QuestionCreationResult.Failure(
                        $"Unknown question type '{input.QuestionType}'. Use MultipleChoice, TrueFalse, or ShortAnswer.")
            };

        // Copies the validated content from a freshly built (transient) question onto the existing
        // tracked one, preserving its Id. Called only when the two are the same concrete type, so
        // the type checked branches always match.
        private static void ApplyEditedContent(Question target, Question source)
        {
            target.Prompt = source.Prompt;
            target.Points = source.Points;
            switch (target)
            {
                case MultipleChoiceQuestion mc when source is MultipleChoiceQuestion mcSource:
                    mc.Options = mcSource.Options;
                    mc.CorrectOptionIndex = mcSource.CorrectOptionIndex;
                    break;
                case TrueFalseQuestion tf when source is TrueFalseQuestion tfSource:
                    tf.CorrectAnswer = tfSource.CorrectAnswer;
                    break;
                case ShortAnswerQuestion sa when source is ShortAnswerQuestion saSource:
                    sa.CorrectAnswerText = saSource.CorrectAnswerText;
                    break;
            }
        }

        private QuizDto MapToDto(Quiz quiz, bool isLocked)
        {
            return new QuizDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                DurationMinutes = quiz.DurationMinutes,
                ClassroomId = quiz.ClassroomId,
                TeacherId = quiz.CreatedByTeacherId,
                IsPublished = quiz.IsPublished,
                AvailableFrom = quiz.AvailableFrom,
                AvailableTo = quiz.AvailableTo,
                MaxAttempts = quiz.MaxAttempts,
                IsLocked = isLocked,
                Questions = quiz.Questions.Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Prompt = q.Prompt,
                    Points = q.Points,
                    QuestionType = q.QuestionType,
                    Options = (q as MultipleChoiceQuestion)?.Options
                }).ToList()
            };
        }
    }
}
