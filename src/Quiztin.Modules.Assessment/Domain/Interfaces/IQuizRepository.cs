using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.Interfaces
{
    public interface IQuizRepository
    {
        Task AddAsync(Quiz quiz);
        Task<Quiz?> GetByIdAsync(Guid id);
        Task UpdateAsync(Quiz quiz);
        Task<Classroom?> GetClassroomAsync(Guid classroomId);
        Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid classroomId);

        /// <summary>
        /// The quizzes a student may take right now (spec 0006, AC-1): published, inside the
        /// availability window, and only in classrooms they are enrolled in. Scoped by the
        /// student's own id from the token, so it can never surface another classroom's work.
        /// </summary>
        Task<(IReadOnlyList<Quiz> Items, int Total)> GetAvailableForStudentAsync(Guid studentId, int skip, int take);

        /// <summary>
        /// Every quiz in a classroom, for the teacher's authoring list (spec 0009, AC-10). Ordered
        /// by title and carrying its questions so the list can show a question count. The classroom
        /// ownership check runs in the application layer before this does.
        /// </summary>
        Task<IReadOnlyList<Quiz>> GetByClassroomAsync(Guid classroomId);
    }
}
