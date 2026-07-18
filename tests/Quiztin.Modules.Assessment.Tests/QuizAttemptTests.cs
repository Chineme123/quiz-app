using System;
using System.Collections.Generic;
using Xunit;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Strategies;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;

namespace Quiztin.Modules.Assessment.Tests
{
    public class QuizAttemptTests
    {
        [Fact]
        public void NewAttempt_ShouldBe_NotStarted()
        {
            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            Assert.Equal("NotStarted", attempt.CurrentStateName);
        }

        [Fact]
        public void Start_ShouldTransitionTo_InProgress_AndPinTheDeadline()
        {
            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            attempt.Start(30);
            Assert.Equal("InProgress", attempt.CurrentStateName);
            Assert.NotEqual(default, attempt.StartedAt);
            // The deadline is pinned from the duration at this instant, so nothing can move
            // it later (spec 0006, AC-3).
            Assert.Equal(attempt.StartedAt.AddMinutes(30), attempt.ExpiresAt);
        }

        [Fact]
        public void Submit_ShouldTransitionTo_Submitted()
        {
            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            attempt.Start(10);
            attempt.Submit();
            Assert.Equal("Submitted", attempt.CurrentStateName);
            Assert.NotNull(attempt.SubmittedAt);
        }

        // Mock strategy for evaluation test
        private class MockScoringStrategy : IScoringStrategy
        {
            public void Score(QuizAttempt attempt, IReadOnlyList<Question> questions)
            {
                attempt.TotalScore = 100;
            }
        }

        [Fact]
        public void Evaluate_ShouldTransitionTo_Graded()
        {
            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            attempt.Start(10);
            attempt.Submit();
            attempt.Evaluate(new MockScoringStrategy(), new List<Question>());

            Assert.Equal("Graded", attempt.CurrentStateName);
            Assert.Equal(100, attempt.TotalScore);
             Assert.NotNull(attempt.GradedAt);
        }

        [Fact]
        public void Questions_GradeTheirOwnSubmittedAnswers()
        {
            var mc = new MultipleChoiceQuestion("Which of these is a colour?", 10, new List<string> { "Dog", "Cat", "Blue" }, 2);
            Assert.True(mc.IsCorrect("2"));      // by index
            Assert.True(mc.IsCorrect("Blue"));   // by option text
            Assert.False(mc.IsCorrect("0"));     // wrong index
            Assert.False(mc.IsCorrect("Cat"));   // wrong option text

            var tf = new TrueFalseQuestion("The sky is blue.", 5, true);
            Assert.True(tf.IsCorrect("true"));
            Assert.False(tf.IsCorrect("false"));

            var sa = new ShortAnswerQuestion("Capital of France?", 5, "Paris");
            Assert.True(sa.IsCorrect("  paris "));   // trimmed + case-insensitive
            Assert.False(sa.IsCorrect("London"));
        }

        [Fact]
        public void PointsScoring_AwardsPointsForCorrectAnswersOnly()
        {
            var mc = new MultipleChoiceQuestion("2 + 2?", 10, new List<string> { "3", "4", "5" }, 1);
            var tf = new TrueFalseQuestion("The sky is blue.", 5, true);
            var sa = new ShortAnswerQuestion("Capital of France?", 5, "Paris");
            var questions = new List<Question> { mc, tf, sa };

            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            attempt.Start(10);
            // Answers reach the attempt as saved drafts now; submit grades what is saved
            // rather than taking a payload (spec 0006, AC-11).
            attempt.SaveDraftAnswers(new Dictionary<Guid, string>
            {
                [mc.Id] = "1",       // correct   -> 10
                [tf.Id] = "false",   // incorrect -> 0
                [sa.Id] = " paris ", // correct   -> 5
            }, DateTime.UtcNow);
            attempt.Submit();

            attempt.Evaluate(new PointsScoringStrategy(), questions);

            Assert.Equal(15m, attempt.TotalScore);
            Assert.Equal("Graded", attempt.CurrentStateName);
        }

        [Fact]
        public void Evaluate_GradesEveryQuestion_IncludingOnesLeftUnanswered()
        {
            var mc = new MultipleChoiceQuestion("2 + 2?", 10, new List<string> { "3", "4", "5" }, 1);
            var tf = new TrueFalseQuestion("The sky is blue.", 5, true);
            var skipped = new ShortAnswerQuestion("Capital of France?", 5, "Paris");
            var questions = new List<Question> { mc, tf, skipped };

            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            attempt.Start(10);
            // Answer the first two, leave the third with no draft at all.
            attempt.SaveDraftAnswers(new Dictionary<Guid, string>
            {
                [mc.Id] = "1",     // correct  -> 10
                [tf.Id] = "true",  // correct  -> 5
            }, DateTime.UtcNow);
            attempt.Submit();

            attempt.Evaluate(new PointsScoringStrategy(), questions);

            // The graded record covers the whole quiz, not only the answered part (spec 0006):
            // the skipped question is a blank answer that scored zero, so the results screen's
            // "N of M" is honest rather than reading "2 of 2 right".
            Assert.Equal(3, attempt.Answers.Count);
            var skippedAnswer = attempt.Answers.Single(a => a.QuestionId == skipped.Id);
            Assert.False(skippedAnswer.IsCorrect);
            Assert.Equal(0m, skippedAnswer.PointsAwarded);
            Assert.Equal(string.Empty, skippedAnswer.ProvidedAnswer);
            Assert.Equal(15m, attempt.TotalScore);
        }
    }
}
