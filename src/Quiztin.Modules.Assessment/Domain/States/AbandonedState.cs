using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.States
{
    public class AbandonedState : QuizAttemptState
    {
        public override string Name => "Abandoned";
        
        // Terminal state
    }
}
