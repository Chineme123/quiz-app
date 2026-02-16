using System;
using System.Threading.Tasks;
using QuizService.Domain.Entities;

namespace QuizService.Domain.Interfaces
{
    public interface IQuizRepository
    {
        Task AddAsync(Quiz quiz);
        Task<Quiz> GetByIdAsync(Guid id);
        Task UpdateAsync(Quiz quiz);
        Task<Classroom> GetClassroomAsync(Guid classroomId);
        Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid classroomId);
        // Additional methods as needed
    }
}
