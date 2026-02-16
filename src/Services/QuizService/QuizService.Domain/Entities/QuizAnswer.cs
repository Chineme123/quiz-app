using System;

namespace QuizService.Domain.Entities
{
    public class QuizAnswer
    {
        public Guid Id { get; private set; }
        public Guid QuizAttemptId { get; private set; }
        public Guid QuestionId { get; private set; }
        public string ProvidedAnswer { get; private set; } // Could be JSON or simple string
        public bool IsCorrect { get; set; } // Set by scoring strategy
        public decimal PointsAwarded { get; set; } // Set by scoring strategy
        public string? Feedback { get; set; } // Set by feedback strategy

        public QuizAnswer(Guid questionId, string providedAnswer)
        {
            Id = Guid.NewGuid();
            QuestionId = questionId;
            ProvidedAnswer = providedAnswer;
        }

        // EF Core constructor
        private QuizAnswer() { }
    }
}
