using System.Collections.Generic;

namespace QuizService.Domain.Entities
{
    public class Classroom
    {
        public Guid Id { get; set; }
        public string TeacherId { get; set; } // From JWT
        public string Name { get; set; }
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

        public Classroom(string teacherId, string name)
        {
             Id = Guid.NewGuid();
             TeacherId = teacherId;
             Name = name;
        }

        // Host for EF
        protected Classroom() { }
    }
}
