using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.States
{
    public class ReviewableState : QuizAttemptState
    {
        public override string Name => "Reviewable";
        
        // Terminal state, no further transitions by default
    }
}
