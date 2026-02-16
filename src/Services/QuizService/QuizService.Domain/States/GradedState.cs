using QuizService.Domain.Entities;

namespace QuizService.Domain.States
{
    public class GradedState : QuizAttemptState
    {
        public override string Name => "Graded";

        public override void GenerateFeedback(QuizAttempt attempt)
        {
            attempt.TransitionTo(new ReviewableState());
        }
    }
}
