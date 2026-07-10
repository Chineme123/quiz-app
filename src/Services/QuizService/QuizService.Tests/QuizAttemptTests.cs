using System;
using System.Collections.Generic;
using Xunit;
using QuizService.Domain.Entities;
using QuizService.Domain.Strategies;
using QuizService.Infrastructure.Strategies;

namespace QuizService.Tests
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
        public void Start_ShouldTransitionTo_InProgress()
        {
            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            attempt.Start();
            Assert.Equal("InProgress", attempt.CurrentStateName);
            Assert.NotEqual(default, attempt.StartedAt);
        }

        [Fact]
        public void Submit_ShouldTransitionTo_Submitted()
        {
            var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid());
            attempt.Start();
            attempt.Submit(new List<QuizAnswer>());
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
            attempt.Start();
            attempt.Submit(new List<QuizAnswer>());
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
            attempt.Start();
            attempt.Submit(new List<QuizAnswer>
            {
                new QuizAnswer(mc.Id, "1"),       // correct  -> 10
                new QuizAnswer(tf.Id, "false"),   // incorrect -> 0
                new QuizAnswer(sa.Id, " paris "), // correct  -> 5
            });

            attempt.Evaluate(new PointsScoringStrategy(), questions);

            Assert.Equal(15m, attempt.TotalScore);
            Assert.Equal("Graded", attempt.CurrentStateName);
        }
    }
}
