using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.States
{
    public class InProgressState : QuizAttemptState
    {
        public override string Name => "InProgress";

        public override void Submit(QuizAttempt attempt)
        {
            attempt.TransitionTo(new SubmittedState());
        }

        public override void Abandon(QuizAttempt attempt)
        {
            attempt.TransitionTo(new AbandonedState());
        }
    }
}
