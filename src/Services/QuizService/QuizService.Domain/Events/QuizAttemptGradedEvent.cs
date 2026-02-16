using System;

namespace QuizService.Domain.Events
{
    public class QuizAttemptGradedEvent
    {
        public Guid AttemptId { get; }
        public Guid StudentId { get; }
        public Guid QuizId { get; }
        public decimal Score { get; }
        public DateTime OccurredOn { get; }

        public QuizAttemptGradedEvent(Guid attemptId, Guid studentId, Guid quizId, decimal score)
        {
            AttemptId = attemptId;
            StudentId = studentId;
            QuizId = quizId;
            Score = score;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
