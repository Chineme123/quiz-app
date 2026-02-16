using System;
using System.Threading.Tasks;
using QuizService.Domain.Entities;

namespace QuizService.Domain.Interfaces
{
    public interface IQuizAttemptRepository
    {
        Task AddAsync(QuizAttempt attempt);
        Task<QuizAttempt> GetByIdAsync(Guid id);
        Task UpdateAsync(QuizAttempt attempt);
        Task<QuizAttempt> FindByStudentAndQuizAsync(Guid studentId, Guid quizId);
        Task<int> GetAttemptCountAsync(Guid studentId, Guid quizId);
        Task<bool> HasCommandBeenProcessedAsync(Guid commandId);
        Task MarkCommandAsProcessedAsync(Guid commandId, string type);
        
        // Transaction support
        Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy GetExecutionStrategy();
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction);
        Task RollbackTransactionAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction);
    }
}
