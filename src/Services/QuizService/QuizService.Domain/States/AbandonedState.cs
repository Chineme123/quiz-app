using QuizService.Domain.Entities;

namespace QuizService.Domain.States
{
    public class AbandonedState : QuizAttemptState
    {
        public override string Name => "Abandoned";
        
        // Terminal state
    }
}
