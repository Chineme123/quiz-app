using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.States
{
    public class SubmittedState : QuizAttemptState
    {
        public override string Name => "Submitted";

        public override void Evaluate(QuizAttempt attempt)
        {
            attempt.TransitionTo(new GradedState());
        }
    }
}
