using System;
using System.Threading.Tasks;
using QuizService.Application.DTOs;

namespace QuizService.Application.Interfaces
{
    public interface IQuizAppService
    {
        Task<QuizDto> CreateQuizAsync(Guid classroomId, string teacherId, CreateQuizDto input);
        Task<QuizDto> AddQuestionAsync(Guid quizId, string teacherId, AddQuestionDto input);
        Task<QuizDto> GenerateQuestionsAsync(Guid quizId, string teacherId, GenerateQuestionsDto input);
        Task<QuizDto> GetQuizAsync(Guid quizId);
    }
}
