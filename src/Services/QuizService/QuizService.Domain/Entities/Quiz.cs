using System;
using System.Collections.Generic;

namespace QuizService.Domain.Entities
{
    public class Quiz
    {
        public Guid Id { get; set; }
        public Guid ClassroomId { get; set; }
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public string CreatedByTeacherId { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableTo { get; set; }
        public int MaxAttempts { get; set; } = 1;
        public ICollection<Question> Questions { get; set; } = new List<Question>();

        public Quiz(Guid classroomId, string title, int durationMinutes, string teacherId)
        {
            Id = Guid.NewGuid();
            ClassroomId = classroomId;
            Title = title;
            DurationMinutes = durationMinutes;
            CreatedByTeacherId = teacherId;
        }

        // Host for EF
        protected Quiz() { }

        public bool CanStart(int currentAttemptCount, out string reason)
        {
            if (!IsPublished)
            {
                reason = "Quiz is not published.";
                return false;
            }

            var now = DateTime.UtcNow;
            if (AvailableFrom.HasValue && now < AvailableFrom.Value)
            {
                reason = "Quiz is not yet available.";
                return false;
            }

            if (AvailableTo.HasValue && now > AvailableTo.Value)
            {
                reason = "Quiz is no longer available.";
                return false;
            }

            if (currentAttemptCount >= MaxAttempts)
            {
                reason = $"Maximum attempts ({MaxAttempts}) exceeded.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
