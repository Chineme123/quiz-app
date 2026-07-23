using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Results;

namespace Quiztin.Modules.Assessment.Application.Interfaces
{
    public interface IQuizAppService
    {
        /// <summary>
        /// Creates a quiz in a classroom the teacher owns (spec 0009). Reports NotFound for a
        /// non owner or a missing classroom, so a classroom's existence never leaks, and Invalid
        /// for a blank title or a non positive duration.
        /// </summary>
        Task<AuthoringResult> CreateQuizAsync(Guid classroomId, Guid teacherId, CreateQuizDto input);

        /// <summary>
        /// Adds a question to a quiz the teacher owns (spec 0009, AC-3). Validated by the same
        /// QuestionFactory rules a generated question is. NotFound for a non owner, Invalid for a
        /// malformed question, Locked (409) once the quiz has any attempt (AC-9).
        /// </summary>
        Task<AuthoringResult> AddQuestionAsync(Guid quizId, Guid teacherId, AddQuestionDto input);

        /// <summary>
        /// Edits one of a quiz's questions in place (spec 0009, AC-3). Same rules and outcomes as
        /// add. The question's type cannot change here (that is a delete then add), so a type that
        /// differs from the stored one is Invalid.
        /// </summary>
        Task<AuthoringResult> EditQuestionAsync(Guid quizId, Guid questionId, Guid teacherId, AddQuestionDto input);

        /// <summary>
        /// Deletes one of a quiz's questions (spec 0009, AC-3). NotFound for a non owner or a
        /// missing question, Locked (409) once the quiz has any attempt (AC-9).
        /// </summary>
        Task<AuthoringResult> DeleteQuestionAsync(Guid quizId, Guid questionId, Guid teacherId);

        /// <summary>
        /// The full quiz for editing (spec 0009, AC-10): questions, settings, publish state, and
        /// the locked flag. Owner scoped: null for a non owner or a missing quiz, so existence
        /// never leaks. This replaces the old unscoped read.
        /// </summary>
        Task<QuizDto?> GetQuizForEditingAsync(Guid quizId, Guid teacherId);

        /// <summary>
        /// The teacher's quizzes in one classroom they own (spec 0009, AC-10): id, title, publish
        /// state, question count, attempt count. Null for a non owner or a missing classroom.
        /// </summary>
        Task<IReadOnlyList<QuizSummaryDto>?> GetQuizzesForClassroomAsync(Guid classroomId, Guid teacherId);

        Task<QuizDto> GenerateQuestionsAsync(Guid quizId, Guid teacherId, GenerateQuestionsDto input);

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
