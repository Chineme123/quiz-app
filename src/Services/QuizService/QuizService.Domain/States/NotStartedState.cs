using QuizService.Domain.Entities;

namespace QuizService.Domain.States
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
