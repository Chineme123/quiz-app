using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Domain.Entities
{
    public class Classroom
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; } // canonical teacher identity (JWT NameIdentifier)
        public string Name { get; set; }
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

        public Classroom(Guid teacherId, string name)
        {
             Id = Guid.NewGuid();
             TeacherId = teacherId;
             Name = name;
        }

        // Host for EF
        protected Classroom() { }
    }
}
