using System;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.States
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

        /// <summary>
        /// Abandon the attempt (foundation §69 triggers 3 and 4, spec 0006). Only a running
        /// attempt can be abandoned; a finished one keeps its result.
        /// </summary>
        public virtual void Abandon(QuizAttempt attempt)
        {
            throw new InvalidOperationException($"Cannot abandon quiz from state {Name}.");
        }
    }
}
