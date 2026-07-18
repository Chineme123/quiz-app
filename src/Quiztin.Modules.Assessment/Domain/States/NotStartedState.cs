using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.States
{
    public class NotStartedState : QuizAttemptState
    {
        public override string Name => "NotStarted";

        public override void Start(QuizAttempt attempt)
        {
            attempt.TransitionTo(new InProgressState());
        }
    }
}
