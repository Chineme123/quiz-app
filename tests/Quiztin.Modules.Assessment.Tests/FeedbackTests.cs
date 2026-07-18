using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Enums;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// The feedback lifecycle on the attempt, the deterministic feedback strategy, and
    /// the correct-answer-text accessor the AI payload uses (spec 0005). Pure domain and
    /// strategy tests, no database.
    /// </summary>
    public class FeedbackTests
    {
        private static QuizAttempt GradedAttempt(out List<Question> questions)
        {
            var mc = new MultipleChoiceQuestion("2 + 2?", 10, new List<string> { "3", "4", "5" }, 1);
            var tf = new TrueFalseQuestion("The sky is blue.", 5, true);
            questions = new List<Question> { mc, tf };

            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            attempt.Start(10);
            attempt.SaveDraftAnswers(new Dictionary<Guid, string>
            {
                [mc.Id] = "1",      // correct
                [tf.Id] = "false",  // incorrect
            }, DateTime.UtcNow);
            attempt.Submit();
            attempt.Evaluate(new PointsScoringStrategy(), questions);
            return attempt;
        }

        [Fact]
        public void NewAttempt_FeedbackStatus_DefaultsToPending()
        {
            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            Assert.Equal(FeedbackStatus.Pending, attempt.FeedbackStatus);
            Assert.Null(attempt.FeedbackGeneratedAt);
        }

        [Fact]
        public void Evaluate_LeavesFeedbackPending()
        {
            var attempt = GradedAttempt(out _);
            Assert.Equal("Graded", attempt.CurrentStateName);
            Assert.Equal(FeedbackStatus.Pending, attempt.FeedbackStatus);
            Assert.Null(attempt.FeedbackGeneratedAt);
        }

        [Fact]
        public void MarkFeedbackReady_MovesToReviewable_AndStampsReady()
        {
            var attempt = GradedAttempt(out _);

            attempt.MarkFeedbackReady();

            Assert.Equal("Reviewable", attempt.CurrentStateName);
            Assert.Equal(FeedbackStatus.Ready, attempt.FeedbackStatus);
            Assert.NotNull(attempt.FeedbackGeneratedAt);
        }

        [Fact]
        public void MarkFeedbackReady_IsIdempotent_WhenAlreadyReady()
        {
            var attempt = GradedAttempt(out _);
            attempt.MarkFeedbackReady();
            var firstStamp = attempt.FeedbackGeneratedAt;

            // A redelivered graded event: calling again is a safe no op, never a second
            // transition or a throw (AC-2, AC-7).
            var replay = Record.Exception(() => attempt.MarkFeedbackReady());

            Assert.Null(replay);
            Assert.Equal("Reviewable", attempt.CurrentStateName);
            Assert.Equal(FeedbackStatus.Ready, attempt.FeedbackStatus);
            Assert.Equal(firstStamp, attempt.FeedbackGeneratedAt);
        }

        [Fact]
        public async Task StandardFeedback_WritesDeterministicFeedback_PerAnswer()
        {
            var attempt = GradedAttempt(out var questions);

            await new StandardFeedbackStrategy().GenerateAsync(attempt, questions);

            foreach (var answer in attempt.Answers)
            {
                Assert.False(string.IsNullOrWhiteSpace(answer.Feedback));
                Assert.Equal(FeedbackSource.Deterministic, answer.FeedbackSource);
            }
            // Correct and incorrect answers get different, encouraging text.
            var correct = attempt.Answers.Single(a => a.IsCorrect);
            var incorrect = attempt.Answers.Single(a => !a.IsCorrect);
            Assert.NotEqual(correct.Feedback, incorrect.Feedback);
            Assert.Contains("another look", incorrect.Feedback, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetCorrectAnswerText_ReturnsPlainText_PerQuestionType()
        {
            var mc = new MultipleChoiceQuestion("Which is a colour?", 1, new List<string> { "Dog", "Cat", "Blue" }, 2);
            Assert.Equal("Blue", mc.GetCorrectAnswerText());

            Assert.Equal("True", new TrueFalseQuestion("The sky is blue.", 1, true).GetCorrectAnswerText());
            Assert.Equal("False", new TrueFalseQuestion("Fish fly.", 1, false).GetCorrectAnswerText());

            Assert.Equal("Paris", new ShortAnswerQuestion("Capital of France?", 1, "Paris").GetCorrectAnswerText());
        }
    }
}
