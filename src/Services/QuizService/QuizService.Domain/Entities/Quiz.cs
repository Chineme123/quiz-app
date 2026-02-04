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
    }
}
