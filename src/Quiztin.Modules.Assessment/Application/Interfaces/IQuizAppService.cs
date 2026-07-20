using System;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Results;

namespace Quiztin.Modules.Assessment.Application.Interfaces
{
    public interface IQuizAppService
    {
        Task<QuizDto> CreateQuizAsync(Guid classroomId, Guid teacherId, CreateQuizDto input);
        Task<QuizDto> AddQuestionAsync(Guid quizId, Guid teacherId, AddQuestionDto input);
        Task<QuizDto> GenerateQuestionsAsync(Guid quizId, Guid teacherId, GenerateQuestionsDto input);
        Task<QuizDto> GetQuizAsync(Guid quizId);

        /// <summary>
        /// Publishes a quiz the teacher owns (spec 0009): validates ownership, at least one
        /// question, a sane window, and a positive attempt limit, then writes the window,
        /// attempts, and IsPublished. Reports NotFound for a non owner so existence never leaks.
        /// </summary>
        Task<PublishResult> PublishAsync(Guid quizId, Guid teacherId, PublishQuizDto input);

        /// <summary>Takes a quiz back off the available list. Owner only.</summary>
        Task<PublishResult> UnpublishAsync(Guid quizId, Guid teacherId);
    }
}
