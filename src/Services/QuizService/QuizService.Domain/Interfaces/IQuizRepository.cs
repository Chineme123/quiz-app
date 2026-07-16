using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuizService.Domain.Entities;

namespace QuizService.Domain.Interfaces
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
    }
}
