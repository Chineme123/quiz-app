using System;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.DTOs;

namespace Quiztin.Modules.Assessment.Application.Interfaces
{
    /// <summary>
    /// A student's own results (spec 0011): every quiz they have finished, grouped by class, with a
    /// standing per class. Read only, scoped to the signed in student. There is no student id
    /// parameter, so it can only ever return the caller's own results — another student's are
    /// unreachable by construction (AC-1). An empty result is a normal MyResultsDto with no
    /// classrooms, never null, because a student always owns their own results (AC-8).
    /// </summary>
    public interface IMyResultsAppService
    {
        Task<MyResultsDto> GetMyResultsAsync(Guid studentId);
    }
}
