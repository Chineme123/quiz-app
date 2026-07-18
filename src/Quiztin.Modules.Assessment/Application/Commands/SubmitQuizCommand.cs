using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Application.Commands
{
    /// <summary>
    /// Submits an attempt (spec 0006, AC-11). It carries no answers: they are already on the
    /// server as saved drafts, so this only moves the attempt InProgress to Submitted, which
    /// is where the drafts become answer rows.
    ///
    /// Submitting an attempt that already finished is a deliberate no op, so an ordinary
    /// client retry after a network hiccup is safe. What a no op means for the caller is the
    /// facade's decision, not this command's; an abandoned attempt never reaches here,
    /// because the facade answers that case before building the command (AC-16).
    /// </summary>
    public class SubmitQuizCommand : IQuizCommand
    {
        private readonly QuizAttempt _attempt;

        public SubmitQuizCommand(QuizAttempt attempt)
        {
            _attempt = attempt;
        }

        public Task ExecuteAsync()
        {
            if (_attempt.CurrentStateName is "Submitted" or "Graded" or "Reviewable")
            {
                return Task.CompletedTask;
            }

            _attempt.Submit();
            return Task.CompletedTask;
        }
    }
}
