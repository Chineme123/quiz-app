using System;
using System.Threading.Tasks;
using QuizService.Application.DTOs;

namespace QuizService.Application.Interfaces
{
    public interface IQuizAppService
    {
        Task<QuizDto> CreateQuizAsync(Guid classroomId, Guid teacherId, CreateQuizDto input);
        Task<QuizDto> AddQuestionAsync(Guid quizId, Guid teacherId, AddQuestionDto input);
        Task<QuizDto> GenerateQuestionsAsync(Guid quizId, Guid teacherId, GenerateQuestionsDto input);
        Task<QuizDto> GetQuizAsync(Guid quizId);
    }
}
