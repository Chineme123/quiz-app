using System;
using System.Collections.Generic;
using Xunit;
using QuizService.Domain.Entities;
using QuizService.Domain.Strategies;

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
            public void Score(QuizAttempt attempt)
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
            attempt.Evaluate(new MockScoringStrategy());
            
            Assert.Equal("Graded", attempt.CurrentStateName);
            Assert.Equal(100, attempt.TotalScore);
             Assert.NotNull(attempt.GradedAt);
        }
    }
}
