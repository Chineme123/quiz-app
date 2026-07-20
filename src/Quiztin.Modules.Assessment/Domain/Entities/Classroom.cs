using System.Collections.Generic;
using Quiztin.Modules.Assessment.Domain.Services;

namespace Quiztin.Modules.Assessment.Domain.Entities
{
    public class Classroom
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; } // canonical teacher identity (JWT NameIdentifier)
        public string Name { get; set; }

        // Short human friendly code a student types (or opens via /join/{code}) to enrol.
        // Unique across all classrooms; the application layer regenerates on collision and
        // on an explicit teacher request (spec 0008). Set at construction so every classroom
        // is born joinable; the seeder and the create service may override with a chosen code.
        public string JoinCode { get; set; }

        // Null means active. Set on archive; cleared on unarchive. Archive is reversible and
        // never deletes rows, so student attempt history survives (spec 0008).
        public DateTime? ArchivedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

        public Classroom(Guid teacherId, string name)
        {
             Id = Guid.NewGuid();
             TeacherId = teacherId;
             Name = name;
             JoinCode = JoinCodeGenerator.Generate();
             CreatedAt = DateTime.UtcNow;
        }

        // Host for EF
        protected Classroom() { }
    }
}
