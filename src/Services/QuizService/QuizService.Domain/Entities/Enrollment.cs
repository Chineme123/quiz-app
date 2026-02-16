using System;

namespace QuizService.Domain.Entities
{
    public class Enrollment
    {
        public Guid Id { get; private set; }
        public Guid StudentId { get; private set; }
        public Guid ClassroomId { get; private set; }
        public DateTime EnrolledAt { get; private set; }

        public Enrollment(Guid studentId, Guid classroomId)
        {
            Id = Guid.NewGuid();
            StudentId = studentId;
            ClassroomId = classroomId;
            EnrolledAt = DateTime.UtcNow;
        }

        // Host for EF
        private Enrollment() { }
    }
}
