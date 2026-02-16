using QuizService.Domain.Entities;

namespace QuizService.Domain.States
{
    public class ReviewableState : QuizAttemptState
    {
        public override string Name => "Reviewable";
        
        // Terminal state, no further transitions by default
    }
}
