using QuizService.Domain.Entities;

namespace QuizService.Domain.States
{
    public class InProgressState : QuizAttemptState
    {
        public override string Name => "InProgress";

        public override void Submit(QuizAttempt attempt)
        {
            attempt.TransitionTo(new SubmittedState());
        }
    }
}
