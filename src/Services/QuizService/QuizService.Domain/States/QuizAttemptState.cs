using System;
using QuizService.Domain.Entities;

namespace QuizService.Domain.States
{
    public abstract class QuizAttemptState
    {
        public abstract string Name { get; }

        public virtual void Start(QuizAttempt attempt)
        {
            throw new InvalidOperationException($"Cannot start quiz from state {Name}.");
        }

        public virtual void Submit(QuizAttempt attempt)
        {
            throw new InvalidOperationException($"Cannot submit quiz from state {Name}.");
        }

        public virtual void Evaluate(QuizAttempt attempt)
        {
            throw new InvalidOperationException($"Cannot evaluate quiz from state {Name}.");
        }

        public virtual void GenerateFeedback(QuizAttempt attempt)
        {
            throw new InvalidOperationException($"Cannot generate feedback from state {Name}.");
        }
    }
}
