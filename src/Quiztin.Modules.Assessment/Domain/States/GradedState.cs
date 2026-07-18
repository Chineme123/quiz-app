using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.States
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
